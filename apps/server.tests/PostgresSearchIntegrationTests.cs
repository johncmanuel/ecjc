using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using server.Data;
using server.Data.Models;
using server.Endpoints;
using Xunit;

namespace server.tests;

public class PostgresSearchIntegrationTests
{
    private ApplicationDbContext? CreatePostgresDbContext()
    {
        var connectionString = "Host=localhost;Port=5432;Database=ecjc;Username=ecjc;Password=ecjc";
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        var db = new ApplicationDbContext(options);
        try
        {
            if (db.Database.CanConnect())
            {
                return db;
            }
        }
        catch
        {
            // DB not reachable
        }
        return null;
    }

    [Fact]
    public async Task PostgresFuzzySearch_TrigramSimilarity_ReturnsMatches()
    {
        using var db = CreatePostgresDbContext();
        if (db == null)
        {
            // Skip if live PostgreSQL is not reachable
            return;
        }

        var userId = $"fts_user_{Guid.NewGuid():N}";
        var partnerId = $"fts_partner_{Guid.NewGuid():N}";
        var groupId = Guid.NewGuid();

        try
        {
            db.Users.Add(new User { Id = userId, Email = $"{userId}@test.com", FriendCode = Guid.NewGuid().ToString("N")[..8] });
            db.Users.Add(new User { Id = partnerId, Email = $"{partnerId}@test.com", FriendCode = Guid.NewGuid().ToString("N")[..8] });
            db.Groups.Add(new Group { Id = groupId });
            db.GroupUsers.Add(new GroupUser { GroupId = groupId, UserId = userId });
            db.GroupUsers.Add(new GroupUser { GroupId = groupId, UserId = partnerId });

            var entry1 = new Entry
            {
                Id = Guid.NewGuid(),
                GroupId = groupId,
                AuthorId = userId,
                TextContent = "We went skateboarding and hiking through the dense redwood forest.",
                CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-10)
            };

            var entry2 = new Entry
            {
                Id = Guid.NewGuid(),
                GroupId = groupId,
                AuthorId = userId,
                TextContent = "Baked some sourdough bread and pastries for breakfast today.",
                CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-5)
            };

            db.Entries.AddRange(entry1, entry2);
            await db.SaveChangesAsync();

            var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId) };
            var claimsIdentity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

            // Test 1: Exact keyword match "skateboarding" via fuzzy search
            var res1 = await EntryEndpoints.SearchEntries(groupId, "skateboarding", 0, 50, claimsPrincipal, db);
            var ok1 = Assert.IsType<Ok<EntryEndpoints.PaginatedEntriesResponse>>(res1.Result);
            Assert.Single(ok1.Value!.Items);
            Assert.Equal(entry1.Id, ok1.Value.Items[0].Id);

            // Test 2: Fuzzy typo match "skateboading" should still match "skateboarding" via trigram similarity
            var res2 = await EntryEndpoints.SearchEntries(groupId, "skateboading", 0, 50, claimsPrincipal, db);
            var ok2 = Assert.IsType<Ok<EntryEndpoints.PaginatedEntriesResponse>>(res2.Result);
            Assert.Single(ok2.Value!.Items);
            Assert.Equal(entry1.Id, ok2.Value.Items[0].Id);

            // Test 3: Search for "sourdough"
            var res3 = await EntryEndpoints.SearchEntries(groupId, "sourdough", 0, 50, claimsPrincipal, db);
            var ok3 = Assert.IsType<Ok<EntryEndpoints.PaginatedEntriesResponse>>(res3.Result);
            Assert.Single(ok3.Value!.Items);
            Assert.Equal(entry2.Id, ok3.Value.Items[0].Id);

            // Test 4: Unmatched query
            var res4 = await EntryEndpoints.SearchEntries(groupId, "astronaut", 0, 50, claimsPrincipal, db);
            var ok4 = Assert.IsType<Ok<EntryEndpoints.PaginatedEntriesResponse>>(res4.Result);
            Assert.Empty(ok4.Value!.Items);

            // Test 5: Empty query returns BadRequest
            var res5 = await EntryEndpoints.SearchEntries(groupId, "", 0, 50, claimsPrincipal, db);
            Assert.IsType<BadRequest<UserEndpoints.ErrorResponse>>(res5.Result);
        }
        finally
        {
            // Clean up test data
            var group = await db.Groups.FindAsync(groupId);
            if (group != null) db.Groups.Remove(group);
            var u1 = await db.Users.FindAsync(userId);
            if (u1 != null) db.Users.Remove(u1);
            var u2 = await db.Users.FindAsync(partnerId);
            if (u2 != null) db.Users.Remove(u2);
            await db.SaveChangesAsync();
        }
    }
}
