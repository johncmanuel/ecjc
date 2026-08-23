using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using server.Data;
using server.Data.Models;
using server.Endpoints;
using Xunit;

namespace server.tests;

public class UserEndpointsTests
{
    private ApplicationDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task UpdateMe_UpdatesPaymentHandles_Successfully()
    {
        // Arrange
        var db = GetInMemoryDbContext();
        var userId = "test_user_id";
        db.Users.Add(new User { Id = userId, Email = "test@test.com", FriendCode = "TEST1" });
        await db.SaveChangesAsync();

        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId) };
        var claimsIdentity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

        var request = new UserEndpoints.UpdateProfileRequest(
            Name: null, FirstName: null, LastName: null,
            VenmoHandle: "testvenmo", CashAppHandle: "testcashapp", PayPalHandle: "testpaypal"
        );

        // Act
        var result = await UserEndpoints.UpdateMe(request, claimsPrincipal, db);

        // Assert
        var okResult = Assert.IsType<Ok<UserEndpoints.UserProfileResponse>>(result);
        var response = okResult.Value;

        Assert.Equal("testvenmo", response.VenmoHandle);
        Assert.Equal("testcashapp", response.CashAppHandle);
        Assert.Equal("testpaypal", response.PayPalHandle);

        var dbUser = await db.Users.FindAsync(userId);
        Assert.Equal("testvenmo", dbUser!.VenmoHandle);
        Assert.Equal("testcashapp", dbUser!.CashAppHandle);
        Assert.Equal("testpaypal", dbUser!.PayPalHandle);
    }
}
