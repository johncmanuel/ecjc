using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using server.Data;
using server.Data.Models;
using System.Security.Cryptography;

namespace server.Endpoints;

public static class UserEndpoints
{
    public static void RegisterUserEndpoints(this WebApplication app)
    {
        var endpoints = app.MapGroup("/api/users").WithTags("Users");

        endpoints.MapPost("/sync", SyncUser).WithName("SyncUser");
        endpoints.MapGet("/me", GetMe).RequireAuthorization().WithName("GetMe");
        endpoints.MapGet("/by-code/{code}", GetByCode).RequireAuthorization().WithName("GetByCode");
    }

    internal static async Task<Results<Ok<UserSyncResponse>, BadRequest<ErrorResponse>>> SyncUser(
        UserSyncRequest request,
        ApplicationDbContext db)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return TypedResults.BadRequest(new ErrorResponse("Invalid user payload: Email is required."));
        }

        var existingUser = await db.Users.FindAsync(request.Id);

        if (existingUser is null)
        {
            var nameParts = (request.Name ?? "").Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            var firstName = nameParts.Length > 0 ? nameParts[0] : null;
            var lastName = nameParts.Length > 1 ? nameParts[1] : null;

            var user = new User
            {
                Id = request.Id,
                Email = request.Email,
                FirstName = firstName,
                LastName = lastName,
                Image = request.Image,
                FriendCode = GenerateFriendCode(),
            };

            db.Users.Add(user);
        }
        else
        {
            existingUser.Email = request.Email;
            existingUser.Image = request.Image;

            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                var nameParts = request.Name.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                existingUser.FirstName = nameParts.Length > 0 ? nameParts[0] : existingUser.FirstName;
                existingUser.LastName = nameParts.Length > 1 ? nameParts[1] : existingUser.LastName;
            }
        }

        await db.SaveChangesAsync();

        Console.WriteLine($"[User Sync] id={request.Id} email={request.Email} name={request.Name}");

        return TypedResults.Ok(new UserSyncResponse(true, request.Email));
    }

    internal static async Task<Results<Ok<UserProfileResponse>, NotFound<ErrorResponse>, UnauthorizedHttpResult>> GetMe(
        ClaimsPrincipal claimsUser,
        ApplicationDbContext db)
    {
        var userId = claimsUser.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return TypedResults.Unauthorized();

        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
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

    private static string GenerateFriendCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        return string.Create(32, chars, (span, state) =>
        {
            var bytes = RandomNumberGenerator.GetBytes(32);
            for (int i = 0; i < span.Length; i++)
            {
                span[i] = state[bytes[i] % state.Length];
            }
        });
    }

    internal sealed record UserSyncRequest(string Id, string Email, string? Name, string? Image);
    internal sealed record UserSyncResponse(bool Synced, string Email);
    internal sealed record ErrorResponse(string Error);
    internal sealed record UserProfileResponse(string Id, string Email, string? FirstName, string? LastName, string FriendCode, string? Image, DateTimeOffset CreatedAt);
    internal sealed record UserPublicProfileResponse(string Id, string? FirstName, string? LastName, string? Image);
}
