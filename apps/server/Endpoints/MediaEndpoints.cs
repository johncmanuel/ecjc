using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using server.Data;
using server.Data.Models;
using server.Services;

namespace server.Endpoints;

public static class MediaEndpoints
{
    public static void RegisterMediaEndpoints(this WebApplication app)
    {
        // This group uses the entry ID
        var entryMediaEndpoints = app.MapGroup("/api/entries/{entryId:guid}/media").WithTags("Entry Media").RequireAuthorization();
        entryMediaEndpoints.MapPost("/", UploadMedia).DisableAntiforgery().WithName("UploadMedia"); // required for multipart/form-data in minimal APIs if no AF tokens used

        // This group uses the media ID directly
        var mediaEndpoints = app.MapGroup("/api/media").WithTags("Media").RequireAuthorization();
        mediaEndpoints.MapDelete("/{id:guid}", DeleteMedia).WithName("DeleteMedia");
    }

    internal static async Task<Results<Ok<EntryEndpoints.MediaResponse>, BadRequest<UserEndpoints.ErrorResponse>, NotFound<UserEndpoints.ErrorResponse>, UnauthorizedHttpResult>> UploadMedia(
        Guid entryId,
        IFormFile file,
        ClaimsPrincipal claimsUser,
        ApplicationDbContext db,
        IStorageService storageService)
    {
        var userId = claimsUser.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return TypedResults.Unauthorized();

        var entry = await db.Entries
            .Include(e => e.MediaAttachments)
            .FirstOrDefaultAsync(e => e.Id == entryId);

        if (entry is null) return TypedResults.NotFound(new UserEndpoints.ErrorResponse("Entry not found."));
        if (entry.AuthorId != userId) return TypedResults.NotFound(new UserEndpoints.ErrorResponse("Entry not found."));

        if (entry.MediaAttachments.Count >= 4)
            return TypedResults.BadRequest(new UserEndpoints.ErrorResponse("Maximum of 4 media attachments allowed per entry."));

        if (file.Length > 20 * 1024 * 1024) // 20 MB
            return TypedResults.BadRequest(new UserEndpoints.ErrorResponse("File size exceeds 20MB limit."));

        var mediaType = DetermineMediaType(file.ContentType);

        using var stream = file.OpenReadStream();
        var filePath = await storageService.UploadFileAsync(stream, file.FileName, file.ContentType);

        var mediaAttachment = new MediaAttachment
        {
            EntryId = entryId,
            FilePath = filePath,
            FileSize = file.Length,
            MediaType = mediaType
        };

        db.MediaAttachments.Add(mediaAttachment);
        await db.SaveChangesAsync();

        return TypedResults.Ok(new EntryEndpoints.MediaResponse(mediaAttachment.Id, mediaAttachment.FilePath, mediaAttachment.MediaType.ToString()));
    }

    internal static async Task<Results<NoContent, NotFound<UserEndpoints.ErrorResponse>, UnauthorizedHttpResult>> DeleteMedia(
        Guid id,
        ClaimsPrincipal claimsUser,
        ApplicationDbContext db,
        IStorageService storageService)
    {
        var userId = claimsUser.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return TypedResults.Unauthorized();

        var media = await db.MediaAttachments
            .Include(m => m.Entry)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (media is null) return TypedResults.NotFound(new UserEndpoints.ErrorResponse("Media not found."));
        if (media.Entry.AuthorId != userId) return TypedResults.NotFound(new UserEndpoints.ErrorResponse("Media not found."));

        // delete from storage then delete from DB
        await storageService.DeleteFileAsync(media.FilePath);
        db.MediaAttachments.Remove(media);
        await db.SaveChangesAsync();

        return TypedResults.NoContent();
    }

    private static MediaType DetermineMediaType(string contentType)
    {
        if (contentType.StartsWith("image/gif")) return MediaType.Gif;
        if (contentType.StartsWith("image/")) return MediaType.Image;
        if (contentType.StartsWith("video/")) return MediaType.Video;
        return MediaType.Other;
    }
}
