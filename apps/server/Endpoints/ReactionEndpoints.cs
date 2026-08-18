using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using server.Data;
using server.Data.Models;

namespace server.Endpoints;

public static class ReactionEndpoints
{
    public static void RegisterReactionEndpoints(this WebApplication app)
    {
        var endpoints = app.MapGroup("/api/entries/{entryId:guid}/reactions").WithTags("Reactions").RequireAuthorization();

        endpoints.MapPost("/", AddReaction).WithName("AddReaction");
        endpoints.MapDelete("/{emojiCode}", RemoveReaction).WithName("RemoveReaction");
    }

    internal static async Task<Results<Ok<EntryEndpoints.ReactionResponse>, BadRequest<UserEndpoints.ErrorResponse>, NotFound<UserEndpoints.ErrorResponse>, UnauthorizedHttpResult>> AddReaction(
        Guid entryId,
        AddReactionRequest request,
        ClaimsPrincipal claimsUser,
        ApplicationDbContext db)
    {
        var userId = claimsUser.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return TypedResults.Unauthorized();

        var entry = await db.Entries.FirstOrDefaultAsync(e => e.Id == entryId);
        if (entry is null) return TypedResults.NotFound(new UserEndpoints.ErrorResponse("Entry not found."));

        var isMember = await db.GroupUsers.AnyAsync(gu => gu.GroupId == entry.GroupId && gu.UserId == userId);
        if (!isMember) return TypedResults.NotFound(new UserEndpoints.ErrorResponse("Entry not found."));

        if (string.IsNullOrWhiteSpace(request.EmojiCode) || request.EmojiCode.Length > 64)
            return TypedResults.BadRequest(new UserEndpoints.ErrorResponse("Invalid emoji code."));

        var existingReaction = await db.Reactions.FirstOrDefaultAsync(r => r.EntryId == entryId && r.UserId == userId && r.EmojiCode == request.EmojiCode);
        if (existingReaction is not null)
            return TypedResults.BadRequest(new UserEndpoints.ErrorResponse("You have already added this reaction."));

        var reaction = new Reaction
        {
            EntryId = entryId,
            UserId = userId,
            EmojiCode = request.EmojiCode
        };

        db.Reactions.Add(reaction);
        await db.SaveChangesAsync();

        return TypedResults.Ok(new EntryEndpoints.ReactionResponse(reaction.Id, reaction.EmojiCode, reaction.UserId));
    }

    internal static async Task<Results<NoContent, NotFound<UserEndpoints.ErrorResponse>, UnauthorizedHttpResult>> RemoveReaction(
        Guid entryId,
        string emojiCode,
        ClaimsPrincipal claimsUser,
        ApplicationDbContext db)
    {
        var userId = claimsUser.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return TypedResults.Unauthorized();

        var reaction = await db.Reactions.FirstOrDefaultAsync(r => r.EntryId == entryId && r.UserId == userId && r.EmojiCode == emojiCode);
        if (reaction is null) return TypedResults.NotFound(new UserEndpoints.ErrorResponse("Reaction not found."));

        db.Reactions.Remove(reaction);
        await db.SaveChangesAsync();

        return TypedResults.NoContent();
    }

    internal sealed record AddReactionRequest(string EmojiCode);
}
