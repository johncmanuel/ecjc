using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using server.Data;
using server.Data.Models;
using server.Services;

namespace server.Endpoints;

public static class InviteEndpoints
{
    public static void RegisterInviteEndpoints(this WebApplication app)
    {
        var endpoints = app.MapGroup("/api/invites").WithTags("Invites").RequireAuthorization();

        endpoints.MapGet("/pending", GetPendingInvites).WithName("GetPendingInvites");
        endpoints.MapGet("/sent", GetSentInvites).WithName("GetSentInvites");
        endpoints.MapPost("/{id:guid}/accept", AcceptInvite).WithName("AcceptInvite");
        endpoints.MapPost("/{id:guid}/decline", DeclineInvite).WithName("DeclineInvite");
        endpoints.MapPost("/{id:guid}/cancel", CancelInvite).WithName("CancelInvite");
    }

    internal static async Task<Results<Ok<List<PendingInviteResponse>>, UnauthorizedHttpResult>> GetPendingInvites(
        ClaimsPrincipal claimsUser,
        ApplicationDbContext db)
    {
        var userId = claimsUser.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return TypedResults.Unauthorized();

        var invites = await db.GroupInvites
            .Where(gi => gi.InviteeId == userId && gi.Status == InviteStatus.Pending)
            .Include(gi => gi.Inviter)
            .Include(gi => gi.Group)
            .OrderByDescending(gi => gi.CreatedAt)
            .Select(gi => new PendingInviteResponse(
                gi.Id,
                gi.GroupId,
                gi.InviterId,
                gi.Inviter.FirstName,
                gi.Inviter.LastName,
                gi.Inviter.Image,
                gi.CreatedAt
            ))
            .ToListAsync();

        return TypedResults.Ok(invites);
    }

    internal static async Task<Results<Ok<List<SentInviteResponse>>, UnauthorizedHttpResult>> GetSentInvites(
        ClaimsPrincipal claimsUser,
        ApplicationDbContext db)
    {
        var userId = claimsUser.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return TypedResults.Unauthorized();

        var invites = await db.GroupInvites
            .Where(gi => gi.InviterId == userId && gi.Status == InviteStatus.Pending)
            .Include(gi => gi.Invitee)
            .OrderByDescending(gi => gi.CreatedAt)
            .Select(gi => new SentInviteResponse(
                gi.Id,
                gi.GroupId,
                gi.InviteeId,
                gi.Invitee.FirstName,
                gi.Invitee.LastName,
                gi.Invitee.Image,
                gi.CreatedAt
            ))
            .ToListAsync();

        return TypedResults.Ok(invites);
    }

    internal static async Task<Results<Ok<AcceptInviteResponse>, BadRequest<UserEndpoints.ErrorResponse>, NotFound<UserEndpoints.ErrorResponse>, UnauthorizedHttpResult>> AcceptInvite(
        Guid id,
        ClaimsPrincipal claimsUser,
        ApplicationDbContext db,
        CentrifugoService centrifugo)
    {
        var userId = claimsUser.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return TypedResults.Unauthorized();

        var invite = await db.GroupInvites
            .Include(gi => gi.Group)
                .ThenInclude(g => g.GroupUsers)
            .Include(gi => gi.Invitee)
            .FirstOrDefaultAsync(gi => gi.Id == id);

        if (invite is null || invite.InviteeId != userId)
            return TypedResults.NotFound(new UserEndpoints.ErrorResponse("Invite not found."));

        if (invite.Status != InviteStatus.Pending)
            return TypedResults.BadRequest(new UserEndpoints.ErrorResponse("This invite has already been actioned."));

        // add user to the group or clear LeftAt if the user is rejoining
        var existingGroupUser = invite.Group.GroupUsers.FirstOrDefault(gu => gu.UserId == userId);
        if (existingGroupUser != null)
        {
            existingGroupUser.LeftAt = null;
        }
        else
        {
            if (invite.Group.GroupUsers.Count >= 2)
                return TypedResults.BadRequest(new UserEndpoints.ErrorResponse("Group is already at maximum capacity."));

            var groupUser = new GroupUser
            {
                GroupId = invite.GroupId,
                UserId = userId
            };
            db.GroupUsers.Add(groupUser);
        }

        invite.Status = InviteStatus.Accepted;

        // Cancel any other pending invites for this group to prevent race conditions
        var siblingInvites = await db.GroupInvites
            .Where(gi => gi.GroupId == invite.GroupId && gi.Id != invite.Id && gi.Status == InviteStatus.Pending)
            .ToListAsync();
            
        foreach(var sibling in siblingInvites)
        {
            db.GroupInvites.Remove(sibling);
            await centrifugo.PublishToUserAsync(sibling.InviteeId, new
            {
                type = "InviteCancelled",
                inviteId = sibling.Id,
                groupId = sibling.GroupId
            });
        }

        await db.SaveChangesAsync();
        await centrifugo.PublishToUserAsync(invite.InviterId, new
        {
            type = "InviteAccepted",
            inviteId = invite.Id,
            groupId = invite.GroupId,
            userId,
            userName = $"{invite.Invitee.FirstName} {invite.Invitee.LastName}".Trim()
        });

        return TypedResults.Ok(new AcceptInviteResponse(invite.GroupId));
    }

    internal static async Task<Results<NoContent, BadRequest<UserEndpoints.ErrorResponse>, NotFound<UserEndpoints.ErrorResponse>, UnauthorizedHttpResult>> DeclineInvite(
        Guid id,
        ClaimsPrincipal claimsUser,
        ApplicationDbContext db,
        CentrifugoService centrifugo)
    {
        var userId = claimsUser.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return TypedResults.Unauthorized();

        var invite = await db.GroupInvites
            .Include(gi => gi.Invitee)
            .FirstOrDefaultAsync(gi => gi.Id == id);

        if (invite is null || invite.InviteeId != userId)
            return TypedResults.NotFound(new UserEndpoints.ErrorResponse("Invite not found."));

        if (invite.Status != InviteStatus.Pending)
            return TypedResults.BadRequest(new UserEndpoints.ErrorResponse("This invite has already been actioned."));

        invite.Status = InviteStatus.Declined;
        await db.SaveChangesAsync();

        await centrifugo.PublishToUserAsync(invite.InviterId, new
        {
            type = "InviteDeclined",
            inviteId = invite.Id,
            groupId = invite.GroupId,
            userId,
            userName = $"{invite.Invitee.FirstName} {invite.Invitee.LastName}".Trim()
        });

        return TypedResults.NoContent();
    }

    internal static async Task<Results<NoContent, BadRequest<UserEndpoints.ErrorResponse>, NotFound<UserEndpoints.ErrorResponse>, UnauthorizedHttpResult>> CancelInvite(
        Guid id,
        ClaimsPrincipal claimsUser,
        ApplicationDbContext db,
        CentrifugoService centrifugo)
    {
        var userId = claimsUser.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return TypedResults.Unauthorized();

        var invite = await db.GroupInvites
            .Include(gi => gi.Group)
                .ThenInclude(g => g.GroupUsers)
            .FirstOrDefaultAsync(gi => gi.Id == id);

        if (invite is null || invite.InviterId != userId)
            return TypedResults.NotFound(new UserEndpoints.ErrorResponse("Invite not found."));

        if (invite.Status != InviteStatus.Pending)
            return TypedResults.BadRequest(new UserEndpoints.ErrorResponse("Only pending invites can be cancelled."));

        db.GroupInvites.Remove(invite);

        // If this group was just created for this invite (1 member), clean it up.
        // It's technically safe to just check if count is 1. Re-invites to paused groups have count 2.
        if (invite.Group.GroupUsers.Count == 1)
        {
            var pendingCount = await db.GroupInvites.CountAsync(gi => gi.GroupId == invite.GroupId && gi.Status == InviteStatus.Pending && gi.Id != invite.Id);
            if (pendingCount == 0)
            {
                db.Groups.Remove(invite.Group);
            }
        }

        await db.SaveChangesAsync();
        await centrifugo.PublishToUserAsync(invite.InviteeId, new
        {
            type = "InviteCancelled",
            inviteId = invite.Id,
            groupId = invite.GroupId
        });

        return TypedResults.NoContent();
    }

    internal sealed record PendingInviteResponse(
        Guid Id,
        Guid GroupId,
        string InviterId,
        string? InviterFirstName,
        string? InviterLastName,
        string? InviterImage,
        DateTimeOffset CreatedAt);
        
    internal sealed record SentInviteResponse(
        Guid Id,
        Guid GroupId,
        string InviteeId,
        string? InviteeFirstName,
        string? InviteeLastName,
        string? InviteeImage,
        DateTimeOffset CreatedAt);

    internal sealed record AcceptInviteResponse(Guid GroupId);
}
