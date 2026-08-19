using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using server.Data;
using server.Data.Models;
using server.Endpoints;

namespace server.tests;

public class StreakTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory = factory;

    private async Task<(HttpClient Client, Guid GroupId)> SetupTestGroupAsync()
    {
        var client = _factory.CreateClient();
        
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        db.Database.EnsureDeleted();
        db.Database.EnsureCreated();

        var groupId = Guid.NewGuid();
        var partnerId = "partner-user-id";

        db.Users.Add(new User { Id = TestAuthHandler.DefaultUserId, Email = "test@test.com", FriendCode = "11111" });
        db.Users.Add(new User { Id = partnerId, Email = "partner@test.com", FriendCode = "22222" });

        var group = new Group { Id = groupId };
        db.Groups.Add(group);

        db.GroupUsers.Add(new GroupUser { GroupId = groupId, UserId = TestAuthHandler.DefaultUserId });
        db.GroupUsers.Add(new GroupUser { GroupId = groupId, UserId = partnerId });

        await db.SaveChangesAsync();

        return (client, groupId);
    }

    [Fact]
    public async Task CreateEntry_FirstPost_SetsStreakToOne()
    {
        var (client, groupId) = await SetupTestGroupAsync();
        
        // Reset TimeProvider to a fixed date
        var current = _factory.TimeProvider.GetUtcNow();
        var nextCleanDay = new DateTimeOffset(current.Year, current.Month, current.Day, 12, 0, 0, TimeSpan.Zero).AddDays(1);
        _factory.TimeProvider.SetUtcNow(nextCleanDay);

        var request = new EntryEndpoints.CreateEntryRequest("This is a valid test entry with more than ten words here to pass validation.");
        var response = await client.PostAsJsonAsync($"/api/groups/{groupId}/entries", request);
        
        response.EnsureSuccessStatusCode();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var group = await db.Groups.FindAsync(groupId);

        Assert.NotNull(group);
        Assert.Equal(1, group.StreakCount);
    }

    [Fact]
    public async Task CreateEntry_SameDayPost_KeepsStreakAtOne()
    {
        var (client, groupId) = await SetupTestGroupAsync();
        
        var current = _factory.TimeProvider.GetUtcNow();
        var nextCleanDay = new DateTimeOffset(current.Year, current.Month, current.Day, 12, 0, 0, TimeSpan.Zero).AddDays(1);
        _factory.TimeProvider.SetUtcNow(nextCleanDay);
        var request = new EntryEndpoints.CreateEntryRequest("This is a valid test entry with more than ten words here to pass validation.");
        
        var response1 = await client.PostAsJsonAsync($"/api/groups/{groupId}/entries", request);
        response1.EnsureSuccessStatusCode();

        // Advance time by a few hours, still same UTC day
        _factory.TimeProvider.Advance(TimeSpan.FromHours(5));

        var response2 = await client.PostAsJsonAsync($"/api/groups/{groupId}/entries", request);
        response2.EnsureSuccessStatusCode();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var group = await db.Groups.FindAsync(groupId);

        Assert.NotNull(group);
        Assert.Equal(1, group.StreakCount);
    }

    [Fact]
    public async Task CreateEntry_NextDayPost_IncrementsStreak()
    {
        var (client, groupId) = await SetupTestGroupAsync();
        
        var current = _factory.TimeProvider.GetUtcNow();
        var nextCleanDay = new DateTimeOffset(current.Year, current.Month, current.Day, 12, 0, 0, TimeSpan.Zero).AddDays(1);
        _factory.TimeProvider.SetUtcNow(nextCleanDay);
        var request = new EntryEndpoints.CreateEntryRequest("This is a valid test entry with more than ten words here to pass validation.");
        
        // Day 1
        var response1 = await client.PostAsJsonAsync($"/api/groups/{groupId}/entries", request);
        response1.EnsureSuccessStatusCode();

        // Advance to Day 2
        _factory.TimeProvider.Advance(TimeSpan.FromDays(1));

        // Day 2
        var response2 = await client.PostAsJsonAsync($"/api/groups/{groupId}/entries", request);
        response2.EnsureSuccessStatusCode();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var group = await db.Groups.FindAsync(groupId);

        Assert.NotNull(group);
        Assert.Equal(2, group.StreakCount);
    }

    [Fact]
    public async Task CreateEntry_MissedDayPost_ResetsStreakToOne()
    {
        var (client, groupId) = await SetupTestGroupAsync();
        
        var current = _factory.TimeProvider.GetUtcNow();
        var nextCleanDay = new DateTimeOffset(current.Year, current.Month, current.Day, 12, 0, 0, TimeSpan.Zero).AddDays(1);
        _factory.TimeProvider.SetUtcNow(nextCleanDay);
        var request = new EntryEndpoints.CreateEntryRequest("This is a valid test entry with more than ten words here to pass validation.");
        
        // Day 1
        var response1 = await client.PostAsJsonAsync($"/api/groups/{groupId}/entries", request);
        response1.EnsureSuccessStatusCode();

        // Advance to Day 2
        _factory.TimeProvider.Advance(TimeSpan.FromDays(1));
        var response2 = await client.PostAsJsonAsync($"/api/groups/{groupId}/entries", request);
        response2.EnsureSuccessStatusCode();
        
        // Assert streak is 2
        using (var scope1 = _factory.Services.CreateScope())
        {
            var db1 = scope1.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var group1 = await db1.Groups.FindAsync(groupId);
            Assert.Equal(2, group1?.StreakCount);
        }

        // Advance to Day 4 (Missed Day 3)
        _factory.TimeProvider.Advance(TimeSpan.FromDays(2));

        // Day 4
        var response3 = await client.PostAsJsonAsync($"/api/groups/{groupId}/entries", request);
        response3.EnsureSuccessStatusCode();

        using (var scope2 = _factory.Services.CreateScope())
        {
            var db2 = scope2.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var group2 = await db2.Groups.FindAsync(groupId);

            Assert.NotNull(group2);
            Assert.Equal(1, group2.StreakCount);
        }
    }
}
