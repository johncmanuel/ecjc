namespace server.Endpoints;

using Microsoft.AspNetCore.Mvc;
using server.Data;
using System.Security.Claims;
using NSwag.Annotations;

public static class SettingsEndpoints
{
    private readonly static int _minPenaltyCents = 500; 
    private readonly static int _maxPenaltyCents = 2000;

    public static void RegisterSettingsEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/settings").WithTags("Settings").RequireAuthorization();

        group.MapPost("/penalty", UpdatePenaltySettings)
            .WithSummary("Update financial penalty settings");
    }

    public class UpdatePenaltyRequest
    {
        public bool IsPenaltyEnabled { get; set; }
        public int PenaltyAmountCents { get; set; } 
    }

    [OpenApiOperation("UpdatePenaltySettings", "Updates the user's financial penalty settings")]
    private static async Task<Microsoft.AspNetCore.Http.HttpResults.Results<Microsoft.AspNetCore.Http.HttpResults.Ok, Microsoft.AspNetCore.Http.HttpResults.UnauthorizedHttpResult, Microsoft.AspNetCore.Http.HttpResults.NotFound>> UpdatePenaltySettings(
        [FromBody] UpdatePenaltyRequest req,
        ApplicationDbContext db,
        ClaimsPrincipal userClaims)
    {
        var userId = userClaims.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return TypedResults.Unauthorized();

        var user = await db.Users.FindAsync(userId);
        if (user == null) return TypedResults.NotFound();

        var amount = req.PenaltyAmountCents;
        if (req.IsPenaltyEnabled)
        {
            if (amount < _minPenaltyCents) amount = _minPenaltyCents;
            if (amount > _maxPenaltyCents) amount = _maxPenaltyCents;
        }

        user.IsPenaltyEnabled = req.IsPenaltyEnabled;
        user.PenaltyAmount = amount;
        
        await db.SaveChangesAsync();
        return TypedResults.Ok();
    }
}
