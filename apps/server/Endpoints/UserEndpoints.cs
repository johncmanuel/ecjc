using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using server.Data;

namespace server.Endpoints;

public static class UserEndpoints
{
    public static void RegisterUserEndpoints(this WebApplication app)
    {
        var endpoints = app.MapGroup("/api/users").WithTags("Users");

        endpoints.MapGet("/me", GetMe).RequireAuthorization().WithName("GetMe");
        endpoints.MapGet("/by-code/{code}", GetByCode).RequireAuthorization().WithName("GetByCode");
    }

    internal static async Task<Results<Ok<UserProfileResponse>, NotFound<ErrorResponse>, UnauthorizedHttpResult>> GetMe(
        ClaimsPrincipal claimsUser,
        ApplicationDbContext db)
    {
        var userId = claimsUser.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return TypedResults.Unauthorized();

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null) return TypedResults.NotFound(new ErrorResponse("User not found in database."));


        return TypedResults.Ok(new UserProfileResponse(
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            user.FriendCode,
            user.Image,
            user.CreatedAt
        ));
    }

    internal static async Task<Results<Ok<UserPublicProfileResponse>, NotFound<ErrorResponse>>> GetByCode(
        string code,
        ApplicationDbContext db)
    {
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.FriendCode == code);
        if (user is null) return TypedResults.NotFound(new ErrorResponse("No user found with that friend code."));

        return TypedResults.Ok(new UserPublicProfileResponse(
            user.Id,
            user.FirstName,
            user.LastName,
            user.Image
        ));
    }

    internal sealed record ErrorResponse(string Error);
    internal sealed record UserProfileResponse(string Id, string Email, string? FirstName, string? LastName, string FriendCode, string? Image, DateTimeOffset CreatedAt);
    internal sealed record UserPublicProfileResponse(string Id, string? FirstName, string? LastName, string? Image);
}
