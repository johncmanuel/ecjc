using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using server.Data;
using server.Data.Models;
using server.Endpoints;
using Xunit;

namespace server.tests;

public class EntryEndpointsTests
{
    private ApplicationDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task SearchEntries_WithQuery_FiltersCorrectly()
    {
        // Arrange
        var db = GetInMemoryDbContext();
        var userId = "test_user";
        var groupId = Guid.NewGuid();

        db.Users.Add(new User { Id = userId, Email = "user@test.com", FriendCode = "123" });
        db.Groups.Add(new Group { Id = groupId });
        db.GroupUsers.Add(new GroupUser { GroupId = groupId, UserId = userId });

        db.Entries.AddRange(
            new Entry
            {
                Id = Guid.NewGuid(),
                GroupId = groupId,
                AuthorId = userId,
                TextContent = "Went hiking in the beautiful green mountains today.",
                CreatedAt = DateTimeOffset.UtcNow.AddHours(-2)
            },
            new Entry
            {
                Id = Guid.NewGuid(),
                GroupId = groupId,
                AuthorId = userId,
                TextContent = "Cooking some delicious Italian pasta for dinner.",
                CreatedAt = DateTimeOffset.UtcNow.AddHours(-1)
            }
        );
        await db.SaveChangesAsync();

        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId) };
        var claimsIdentity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

        // Act - Search for "hiking"
        var result = await EntryEndpoints.SearchEntries(groupId, "hiking", 0, 50, claimsPrincipal, db);

        // Assert
        var okResult = Assert.IsType<Ok<EntryEndpoints.PaginatedEntriesResponse>>(result.Result);
        var response = okResult.Value;

        Assert.NotNull(response);
        Assert.Equal(1, response.TotalCount);
        Assert.Single(response.Items);
        Assert.Contains("hiking", response.Items[0].TextContent);
    }

    [Fact]
    public async Task SearchEntries_WithEmptyQuery_ReturnsBadRequest()
    {
        // Arrange
        var db = GetInMemoryDbContext();
        var userId = "test_user";
        var groupId = Guid.NewGuid();

        db.Users.Add(new User { Id = userId, Email = "user@test.com", FriendCode = "456" });
        db.Groups.Add(new Group { Id = groupId });
        db.GroupUsers.Add(new GroupUser { GroupId = groupId, UserId = userId });
        await db.SaveChangesAsync();

        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId) };
        var claimsIdentity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

        // Act - Search with empty query
        var result = await EntryEndpoints.SearchEntries(groupId, "  ", 0, 50, claimsPrincipal, db);

        // Assert
        var badResult = Assert.IsType<BadRequest<UserEndpoints.ErrorResponse>>(result.Result);
        Assert.Equal("Search query cannot be empty.", badResult.Value!.Error);
    }

    [Fact]
    public async Task GetEntries_ReturnsAllEntries()
    {
        // Arrange
        var db = GetInMemoryDbContext();
        var userId = "test_user";
        var groupId = Guid.NewGuid();

        db.Users.Add(new User { Id = userId, Email = "user@test.com", FriendCode = "789" });
        db.Groups.Add(new Group { Id = groupId });
        db.GroupUsers.Add(new GroupUser { GroupId = groupId, UserId = userId });

        db.Entries.AddRange(
            new Entry
            {
                Id = Guid.NewGuid(),
                GroupId = groupId,
                AuthorId = userId,
                TextContent = "Entry one description with at least ten words in it.",
                CreatedAt = DateTimeOffset.UtcNow.AddHours(-2)
            },
            new Entry
            {
                Id = Guid.NewGuid(),
                GroupId = groupId,
                AuthorId = userId,
                TextContent = "Entry two description with at least ten words in it.",
                CreatedAt = DateTimeOffset.UtcNow.AddHours(-1)
            }
        );
        await db.SaveChangesAsync();

        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId) };
        var claimsIdentity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

        // Act - GetEntries no longer has a search param
        var result = await EntryEndpoints.GetEntries(groupId, 0, 50, claimsPrincipal, db);

        // Assert
        var okResult = Assert.IsType<Ok<EntryEndpoints.PaginatedEntriesResponse>>(result.Result);
        var response = okResult.Value;

        Assert.NotNull(response);
        Assert.Equal(2, response.TotalCount);
        Assert.Equal(2, response.Items.Count);
    }
}
