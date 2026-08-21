using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using server.Data;
using server.Data.Models;
using server.Services;
using Stripe;

namespace server.tests;

public class MockStripeService : IStripeService
{
    public List<(string CustomerId, int AmountCents)> Charges = new();

    public Task<PaymentIntent> ChargeCustomerAsync(string customerId, int amountCents, string description = "Penalty")
    {
        Charges.Add((customerId, amountCents));
        return Task.FromResult(new PaymentIntent());
    }

    public Task<SetupIntent> CreateSetupIntentAsync(string customerId)
    {
        return Task.FromResult(new SetupIntent());
    }

    public Task<string> GetOrCreateCustomerAsync(User user)
    {
        return Task.FromResult("cus_mock");
    }
}

public class StreakEvaluationTests
{
    private ApplicationDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task EvaluateDailyStreaks_BothUsersPosted_IncrementsStreak()
    {
        var db = GetInMemoryDbContext();
        var stripeMock = new MockStripeService();
        var logger = NullLogger<StreakEvaluationService>.Instance;
        // Mock centrifugo service simply using null config, wait it might throw if not initialized
        // Better to skip Centrifugo or just pass null if it accepts it. Let's look at CentrifugoService constructor.
        var centrifugo = new CentrifugoService(new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build());
        var evaluator = new StreakEvaluationService(db, stripeMock, centrifugo, logger);

        var groupId = Guid.NewGuid();
        var user1Id = "user1";
        var user2Id = "user2";

        var group = new Group { Id = groupId, StreakCount = 1 };
        db.Groups.Add(group);
        db.Users.Add(new User { Id = user1Id, Email = "1@test.com", FriendCode = "1" });
        db.Users.Add(new User { Id = user2Id, Email = "2@test.com", FriendCode = "2" });
        db.GroupUsers.Add(new GroupUser { GroupId = groupId, UserId = user1Id });
        db.GroupUsers.Add(new GroupUser { GroupId = groupId, UserId = user2Id });

        var targetDate = new DateTime(2023, 10, 10, 0, 0, 0, DateTimeKind.Utc);
        
        db.Entries.Add(new Entry { GroupId = groupId, AuthorId = user1Id, TextContent = "a", CreatedAt = targetDate.AddHours(5) });
        db.Entries.Add(new Entry { GroupId = groupId, AuthorId = user2Id, TextContent = "b", CreatedAt = targetDate.AddHours(10) });
        await db.SaveChangesAsync();

        await evaluator.EvaluateDailyStreaksAsync(targetDate);

        var updatedGroup = await db.Groups.FindAsync(groupId);
        Assert.Equal(2, updatedGroup!.StreakCount);
        Assert.Empty(stripeMock.Charges);
    }

    [Fact]
    public async Task EvaluateDailyStreaks_OneUserMissed_BreaksStreakAndChargesSlacker()
    {
        var db = GetInMemoryDbContext();
        var stripeMock = new MockStripeService();
        var logger = NullLogger<StreakEvaluationService>.Instance;
        var centrifugo = new CentrifugoService(new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build());
        var evaluator = new StreakEvaluationService(db, stripeMock, centrifugo, logger);

        var groupId = Guid.NewGuid();
        var user1Id = "user1";
        var user2Id = "user2";

        var group = new Group { Id = groupId, StreakCount = 5 };
        db.Groups.Add(group);
        db.Users.Add(new User { Id = user1Id, Email = "1@test.com", FriendCode = "1", IsPenaltyEnabled = true, StripeCustomerId = "cus_1", PenaltyAmount = 500 });
        db.Users.Add(new User { Id = user2Id, Email = "2@test.com", FriendCode = "2", IsPenaltyEnabled = true, StripeCustomerId = "cus_2", PenaltyAmount = 500 });
        db.GroupUsers.Add(new GroupUser { GroupId = groupId, UserId = user1Id });
        db.GroupUsers.Add(new GroupUser { GroupId = groupId, UserId = user2Id });

        var targetDate = new DateTime(2023, 10, 10, 0, 0, 0, DateTimeKind.Utc);
        
        // Only User1 posted
        db.Entries.Add(new Entry { GroupId = groupId, AuthorId = user1Id, TextContent = "a", CreatedAt = targetDate.AddHours(5) });
        await db.SaveChangesAsync();

        await evaluator.EvaluateDailyStreaksAsync(targetDate);

        var updatedGroup = await db.Groups.FindAsync(groupId);
        Assert.Equal(0, updatedGroup!.StreakCount); // Streak broken
        
        Assert.Single(stripeMock.Charges);
        Assert.Equal("cus_2", stripeMock.Charges[0].CustomerId); // User 2 is charged
    }

    [Fact]
    public async Task EvaluateDailyStreaks_BothUsersMissed_BreaksStreakAndChargesBoth()
    {
        var db = GetInMemoryDbContext();
        var stripeMock = new MockStripeService();
        var logger = NullLogger<StreakEvaluationService>.Instance;
        var centrifugo = new CentrifugoService(new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build());
        var evaluator = new StreakEvaluationService(db, stripeMock, centrifugo, logger);

        var groupId = Guid.NewGuid();
        var user1Id = "user1";
        var user2Id = "user2";

        var group = new Group { Id = groupId, StreakCount = 10 };
        db.Groups.Add(group);
        db.Users.Add(new User { Id = user1Id, Email = "1@test.com", FriendCode = "1", IsPenaltyEnabled = true, StripeCustomerId = "cus_1", PenaltyAmount = 500 });
        db.Users.Add(new User { Id = user2Id, Email = "2@test.com", FriendCode = "2", IsPenaltyEnabled = true, StripeCustomerId = "cus_2", PenaltyAmount = 500 });
        db.GroupUsers.Add(new GroupUser { GroupId = groupId, UserId = user1Id });
        db.GroupUsers.Add(new GroupUser { GroupId = groupId, UserId = user2Id });

        var targetDate = new DateTime(2023, 10, 10, 0, 0, 0, DateTimeKind.Utc);
        
        // Neither posted on targetDate
        db.Entries.Add(new Entry { GroupId = groupId, AuthorId = user1Id, TextContent = "a", CreatedAt = targetDate.AddDays(-1) });
        await db.SaveChangesAsync();

        await evaluator.EvaluateDailyStreaksAsync(targetDate);

        var updatedGroup = await db.Groups.FindAsync(groupId);
        Assert.Equal(0, updatedGroup!.StreakCount); // Streak broken
        
        Assert.Equal(2, stripeMock.Charges.Count);
        Assert.Contains(stripeMock.Charges, c => c.CustomerId == "cus_1");
        Assert.Contains(stripeMock.Charges, c => c.CustomerId == "cus_2");
    }
}
