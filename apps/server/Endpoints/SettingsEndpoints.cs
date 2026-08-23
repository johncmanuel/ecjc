namespace server.Endpoints;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using server.Data;
using System.Security.Claims;

public static class SettingsEndpoints
{
    private readonly static int _minPenaltyCents = 500; 
    private readonly static int _maxPenaltyCents = 2000;

    public static void RegisterSettingsEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/settings").WithTags("Settings");

        group.MapGet("/penalty", GetPenaltySettings)
            .RequireAuthorization()
            .WithSummary("Get the current user's penalty settings")
            .Produces<PenaltySettingsResponse>();

        group.MapPost("/penalty", UpdatePenaltySettings)
            .RequireAuthorization()
            .WithSummary("Update the current user's penalty settings")
            .Produces(StatusCodes.Status200OK);

        group.MapPost("/penalty/settle", SettleDebt)
            .RequireAuthorization()
            .WithSummary("Manually settle the accumulated penalty debt")
            .Produces(StatusCodes.Status200OK);

        var apiKeysGroup = group.MapGroup("/api-keys").WithTags("API Keys");

        apiKeysGroup.MapGet("/", GetApiKeys)
            .RequireAuthorization()
            .WithSummary("List all API keys for the current user")
            .Produces<List<ApiKeyResponse>>();

        apiKeysGroup.MapPost("/", CreateApiKey)
            .RequireAuthorization()
            .WithSummary("Create a new API key")
            .Produces<CreateApiKeyResponse>();

        apiKeysGroup.MapDelete("/{id}", DeleteApiKey)
            .RequireAuthorization()
            .WithSummary("Revoke an API key")
            .Produces(StatusCodes.Status200OK);
    }

    private static async Task<IResult> GetPenaltySettings(
        ApplicationDbContext db,
        ClaimsPrincipal userClaims)
    {
        var userId = userClaims.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return TypedResults.Unauthorized();

        var user = await db.Users.FindAsync(userId);
        if (user == null) return TypedResults.NotFound();

        return TypedResults.Ok(new PenaltySettingsResponse
        (
            user.IsPenaltyEnabled,
            user.PenaltyAmount,
            user.AccumulatedPenaltyCents
        ));
    }

    private static async Task<IResult> UpdatePenaltySettings(
        [FromBody] UpdatePenaltyRequest req,
        ApplicationDbContext db,
        ClaimsPrincipal userClaims)
    {
        var userId = userClaims.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return TypedResults.Unauthorized();

        var user = await db.Users.FindAsync(userId);
        if (user == null) return TypedResults.NotFound();

        if (req.PenaltyAmountCents < _minPenaltyCents || req.PenaltyAmountCents > _maxPenaltyCents)
        {
            return TypedResults.BadRequest($"Penalty amount must be between {_minPenaltyCents} and {_maxPenaltyCents} cents.");
        }

        user.IsPenaltyEnabled = req.IsPenaltyEnabled;
        user.PenaltyAmount = req.PenaltyAmountCents;

        await db.SaveChangesAsync();

        return TypedResults.Ok();
    }


    private static async Task<IResult> SettleDebt(
        ApplicationDbContext db,
        ClaimsPrincipal userClaims,
        server.Services.CentrifugoService centrifugo)
    {
        var userId = userClaims.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return TypedResults.Unauthorized();

        var user = await db.Users.FindAsync(userId);
        if (user == null) return TypedResults.NotFound();

        if (user.AccumulatedPenaltyCents > 0)
        {
            user.AccumulatedPenaltyCents = 0;
            await db.SaveChangesAsync();
            
            var notificationPayload = new { type = "debt_settled", amount = 0 };
            await centrifugo.PublishAsync($"user#{user.Id}", notificationPayload);
        }

        return TypedResults.Ok();
    }

    private static async Task<IResult> GetApiKeys(
        ApplicationDbContext db,
        ClaimsPrincipal userClaims)
    {
        var userId = userClaims.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return TypedResults.Unauthorized();

        var keys = await db.ApiKeys
            .Where(k => k.UserId == userId)
            .Select(k => new ApiKeyResponse
            (
                k.Id,
                k.Name,
                k.Prefix,
                k.CreatedAt
            ))
            .ToListAsync();

        return TypedResults.Ok(keys);
    }

    private static async Task<IResult> CreateApiKey(
        [FromBody] CreateApiKeyRequest req,
        ApplicationDbContext db,
        ClaimsPrincipal userClaims)
    {
        var userId = userClaims.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return TypedResults.Unauthorized();

        var token = "ecjc_live_" + Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace("+", "-").Replace("/", "_");
        
        // Generate SHA256 hash of the token for storage
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(token));
        var keyHash = Convert.ToBase64String(hashBytes);

        var prefix = token.Substring(0, 15) + "...";

        var apiKey = new server.Data.Models.ApiKey
        {
            UserId = userId,
            Name = string.IsNullOrWhiteSpace(req.Name) ? "API Key" : req.Name,
            KeyHash = keyHash,
            Prefix = prefix,
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.ApiKeys.Add(apiKey);
        await db.SaveChangesAsync();

        return TypedResults.Ok(new CreateApiKeyResponse
        (
            new ApiKeyResponse
            (
                apiKey.Id,
                apiKey.Name,
                apiKey.Prefix,
                apiKey.CreatedAt
            ),
            token
        ));
    }

    private static async Task<IResult> DeleteApiKey(
        string id,
        ApplicationDbContext db,
        ClaimsPrincipal userClaims)
    {
        var userId = userClaims.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return TypedResults.Unauthorized();

        var apiKey = await db.ApiKeys.FirstOrDefaultAsync(k => k.Id == id && k.UserId == userId);
        if (apiKey == null) return TypedResults.NotFound();

        db.ApiKeys.Remove(apiKey);
        await db.SaveChangesAsync();

        return TypedResults.Ok();
    }

    internal sealed record PenaltySettingsResponse(bool IsPenaltyEnabled, int PenaltyAmountCents, int AccumulatedPenaltyCents);
    internal sealed record UpdatePenaltyRequest(bool IsPenaltyEnabled, int PenaltyAmountCents);
    internal sealed record ApiKeyResponse(string Id, string Name, string Prefix, DateTimeOffset CreatedAt);
    internal sealed record CreateApiKeyRequest(string Name);
    internal sealed record CreateApiKeyResponse(ApiKeyResponse KeyDetails, string Token);
}
