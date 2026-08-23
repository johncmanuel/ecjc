namespace server.Endpoints;

using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using server.Data;


public static class UserEndpoints
{
    public static void RegisterUserEndpoints(this WebApplication app)
    {
        var endpoints = app.MapGroup("/api/users").WithTags("Users");

        endpoints.MapGet("/me", GetMe).RequireAuthorization().WithName("GetMe").Produces<UserProfileResponse>();
        endpoints.MapPut("/me", UpdateMe).RequireAuthorization().WithName("UpdateMe").Produces<UserProfileResponse>();
        endpoints.MapGet("/by-code/{code}", GetByCode).RequireAuthorization().WithName("GetByCode").Produces<UserPublicProfileResponse>();
    }

    internal static async Task<IResult> GetMe(
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
            user.CreatedAt,
            user.VenmoHandle,
            user.CashAppHandle,
            user.PayPalHandle
        ));
    }

    internal static async Task<IResult> GetByCode(
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


    internal static async Task<IResult> UpdateMe(
        [Microsoft.AspNetCore.Mvc.FromBody] UpdateProfileRequest req,
        ClaimsPrincipal claimsUser,
        ApplicationDbContext db)
    {
        var userId = claimsUser.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return TypedResults.Unauthorized();

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null) return TypedResults.NotFound(new ErrorResponse("User not found in database."));

        if (req.Name != null) user.Name = req.Name;
        if (req.FirstName != null) user.FirstName = req.FirstName;
        if (req.LastName != null) user.LastName = req.LastName;

        if (req.VenmoHandle != null) user.VenmoHandle = req.VenmoHandle == "" ? null : req.VenmoHandle;
        if (req.CashAppHandle != null) user.CashAppHandle = req.CashAppHandle == "" ? null : req.CashAppHandle;
        if (req.PayPalHandle != null) user.PayPalHandle = req.PayPalHandle == "" ? null : req.PayPalHandle;

        await db.SaveChangesAsync();

        return TypedResults.Ok(new UserProfileResponse(
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            user.FriendCode,
            user.Image,
            user.CreatedAt,
            user.VenmoHandle,
            user.CashAppHandle,
            user.PayPalHandle
        ));
    }

    internal sealed record UpdateProfileRequest(string? Name, string? FirstName, string? LastName, string? VenmoHandle = null, string? CashAppHandle = null, string? PayPalHandle = null);
    internal sealed record ErrorResponse(string Error);
    internal sealed record UserProfileResponse(string Id, string Email, string? FirstName, string? LastName, string FriendCode, string? Image, DateTimeOffset CreatedAt, string? VenmoHandle = null, string? CashAppHandle = null, string? PayPalHandle = null);
    internal sealed record UserPublicProfileResponse(string Id, string? FirstName, string? LastName, string? Image);
}
