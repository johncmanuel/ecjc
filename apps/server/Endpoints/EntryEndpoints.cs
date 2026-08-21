using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using server.Data;
using server.Data.Models;
using server.Services;

namespace server.Endpoints;

public static class EntryEndpoints
{
    private static readonly int _minWordCount = 10;
    private static readonly int _maxWordCount = 10000;

    public static void RegisterEntryEndpoints(this WebApplication app)
    {
        var groupEndpoints = app.MapGroup("/api/groups/{groupId:guid}/entries").WithTags("Group Entries").RequireAuthorization();
        groupEndpoints.MapGet("/", GetEntries).WithName("GetEntries");
        groupEndpoints.MapPost("/", CreateEntry).WithName("CreateEntry");

        var entryEndpoints = app.MapGroup("/api/entries").WithTags("Entries").RequireAuthorization();
        entryEndpoints.MapPut("/{id:guid}", UpdateEntry).WithName("UpdateEntry");
        entryEndpoints.MapDelete("/{id:guid}", DeleteEntry).WithName("DeleteEntry");
    }

    internal static async Task<Results<Ok<PaginatedEntriesResponse>, NotFound<UserEndpoints.ErrorResponse>, UnauthorizedHttpResult>> GetEntries(
        Guid groupId,
        int skip,
        int take,
        ClaimsPrincipal claimsUser,
        ApplicationDbContext db)
    {
        var userId = claimsUser.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return TypedResults.Unauthorized();

        take = Math.Clamp(take, 1, 100);
        skip = Math.Max(0, skip);

        var isMember = await db.GroupUsers.AnyAsync(gu => gu.GroupId == groupId && gu.UserId == userId);
        if (!isMember) return TypedResults.NotFound(new UserEndpoints.ErrorResponse("Group not found."));

        var totalCount = await db.Entries.CountAsync(e => e.GroupId == groupId);

        var entries = await db.Entries
            .AsNoTracking()
            .Where(e => e.GroupId == groupId)
            .OrderByDescending(e => e.CreatedAt)
            .Skip(skip)
            .Take(take)
            .Include(e => e.Author)
            .Include(e => e.MediaAttachments)
            .Include(e => e.Reactions)
            .Select(e => new EntryResponse(
                e.Id,
                e.TextContent,
                e.AuthorId,
                e.Author.FirstName,
                e.Author.LastName,
                e.Author.Image,
                e.CreatedAt,
                e.UpdatedAt,
                e.MediaAttachments.Select(m => new MediaResponse(m.Id, m.FilePath, m.MediaType.ToString())).ToList(),
                e.Reactions.Select(r => new ReactionResponse(r.Id, r.EmojiCode, r.UserId)).ToList()
            ))
            .ToListAsync();

        return TypedResults.Ok(new PaginatedEntriesResponse(entries, totalCount, skip, take));
    }

    internal static async Task<Results<Ok<EntryResponse>, BadRequest<UserEndpoints.ErrorResponse>, NotFound<UserEndpoints.ErrorResponse>, UnauthorizedHttpResult>> CreateEntry(
        Guid groupId,
        CreateEntryRequest request,
        ClaimsPrincipal claimsUser,
        ApplicationDbContext db,
        TimeProvider timeProvider)
    {
        var userId = claimsUser.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return TypedResults.Unauthorized();

        var isMember = await db.GroupUsers.AnyAsync(gu => gu.GroupId == groupId && gu.UserId == userId);
        if (!isMember) return TypedResults.NotFound(new UserEndpoints.ErrorResponse("Group not found."));

        // Block posting until both members have joined
        var memberCount = await db.GroupUsers.CountAsync(gu => gu.GroupId == groupId);
        if (memberCount < 2) return TypedResults.BadRequest(new UserEndpoints.ErrorResponse("You can't post until your partner accepts the invite."));

        var wordCount = request.TextContent.Split([' ', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries).Length;
        if (wordCount < _minWordCount) return TypedResults.BadRequest(new UserEndpoints.ErrorResponse($"Entry must be at least {_minWordCount} words."));
        if (wordCount > _maxWordCount) return TypedResults.BadRequest(new UserEndpoints.ErrorResponse($"Entry cannot exceed {_maxWordCount} words."));

        var entry = new Entry
        {
            GroupId = groupId,
            AuthorId = userId,
            TextContent = request.TextContent
        };

        db.Entries.Add(entry);
        
        var group = await db.Groups.FindAsync(groupId);
        if (group != null)
        {
            var now = timeProvider.GetUtcNow();
            group.UpdatedAt = now;
        }

        await db.SaveChangesAsync();

        var author = await db.Users.FindAsync(userId);

        return TypedResults.Ok(new EntryResponse(
            entry.Id,
            entry.TextContent,
            entry.AuthorId,
            author?.FirstName,
            author?.LastName,
            author?.Image,
            entry.CreatedAt,
            entry.UpdatedAt,
            [],
            []
        ));
    }

    internal static async Task<Results<Ok<EntryResponse>, BadRequest<UserEndpoints.ErrorResponse>, NotFound<UserEndpoints.ErrorResponse>, UnauthorizedHttpResult>> UpdateEntry(
        Guid id,
        UpdateEntryRequest request,
        ClaimsPrincipal claimsUser,
        ApplicationDbContext db)
    {
        var userId = claimsUser.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return TypedResults.Unauthorized();

        var entry = await db.Entries
            .Include(e => e.Author)
            .Include(e => e.MediaAttachments)
            .Include(e => e.Reactions)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (entry is null) return TypedResults.NotFound(new UserEndpoints.ErrorResponse("Entry not found."));
        if (entry.AuthorId != userId) return TypedResults.NotFound(new UserEndpoints.ErrorResponse("Entry not found.")); 

        var wordCount = request.TextContent.Split([' ', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries).Length;
        if (wordCount < _minWordCount) return TypedResults.BadRequest(new UserEndpoints.ErrorResponse($"Entry must be at least {_minWordCount} words."));
        if (wordCount > _maxWordCount) return TypedResults.BadRequest(new UserEndpoints.ErrorResponse($"Entry cannot exceed {_maxWordCount} words."));

        entry.TextContent = request.TextContent;
        await db.SaveChangesAsync();

        return TypedResults.Ok(new EntryResponse(
            entry.Id,
            entry.TextContent,
            entry.AuthorId,
            entry.Author.FirstName,
            entry.Author.LastName,
            entry.Author.Image,
            entry.CreatedAt,
            entry.UpdatedAt,
            entry.MediaAttachments.Select(m => new MediaResponse(m.Id, m.FilePath, m.MediaType.ToString())).ToList(),
            entry.Reactions.Select(r => new ReactionResponse(r.Id, r.EmojiCode, r.UserId)).ToList()
        ));
    }

    internal static async Task<Results<NoContent, NotFound<UserEndpoints.ErrorResponse>, UnauthorizedHttpResult>> DeleteEntry(
        Guid id,
        ClaimsPrincipal claimsUser,
        ApplicationDbContext db)
    {
        var userId = claimsUser.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return TypedResults.Unauthorized();

        var entry = await db.Entries.FirstOrDefaultAsync(e => e.Id == id);
        if (entry is null) return TypedResults.NotFound(new UserEndpoints.ErrorResponse("Entry not found."));
        if (entry.AuthorId != userId) return TypedResults.NotFound(new UserEndpoints.ErrorResponse("Entry not found."));

        db.Entries.Remove(entry);
        await db.SaveChangesAsync();

        return TypedResults.NoContent();
    }

    internal sealed record CreateEntryRequest(string TextContent);
    internal sealed record UpdateEntryRequest(string TextContent);
    internal sealed record MediaResponse(Guid Id, string Url, string MediaType);
    internal sealed record ReactionResponse(Guid Id, string EmojiCode, string UserId);
    internal sealed record EntryResponse(
        Guid Id, 
        string TextContent, 
        string AuthorId, 
        string? AuthorFirstName, 
        string? AuthorLastName, 
        string? AuthorImage,
        DateTimeOffset CreatedAt, 
        DateTimeOffset UpdatedAt,
        List<MediaResponse> Media,
        List<ReactionResponse> Reactions);
    internal sealed record PaginatedEntriesResponse(List<EntryResponse> Items, int TotalCount, int Skip, int Take);
}
