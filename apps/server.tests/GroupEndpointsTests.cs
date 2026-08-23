using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using server.Data;
using server.Data.Models;
using server.Endpoints;
using Xunit;

namespace server.tests;

public class GroupEndpointsTests
{
    private ApplicationDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task GetGroupDetails_ReturnsPaymentHandles_Successfully()
    {
        // Arrange
        var db = GetInMemoryDbContext();
        var currentUserId = "current_user";
        var otherUserId = "other_user";
        var groupId = Guid.NewGuid();

        db.Users.Add(new User { Id = currentUserId, Email = "test1@test.com", FriendCode = "1" });
        db.Users.Add(new User { 
            Id = otherUserId, 
            Email = "test2@test.com", 
            FriendCode = "2",
            VenmoHandle = "othervenmo",
            CashAppHandle = "othercashapp",
            PayPalHandle = "otherpaypal"
        });

        db.Groups.Add(new Group { Id = groupId });
        db.GroupUsers.Add(new GroupUser { GroupId = groupId, UserId = currentUserId });
        db.GroupUsers.Add(new GroupUser { GroupId = groupId, UserId = otherUserId });
        await db.SaveChangesAsync();

        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, currentUserId) };
        var claimsIdentity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

        // Act
        var result = await GroupEndpoints.GetGroupDetails(groupId, claimsPrincipal, db);

        // Assert
        var okResult = Assert.IsType<Ok<GroupEndpoints.GroupDetailsResponse>>(result.Result);
        var response = okResult.Value;

        Assert.NotNull(response);
        Assert.Equal(2, response.Members.Count);

        var otherMember = response.Members.First(m => m.UserId == otherUserId);
        Assert.Equal("othervenmo", otherMember.VenmoHandle);
        Assert.Equal("othercashapp", otherMember.CashAppHandle);
        Assert.Equal("otherpaypal", otherMember.PayPalHandle);
    }
}
