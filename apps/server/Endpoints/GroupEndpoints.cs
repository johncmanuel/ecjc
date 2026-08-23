using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using server.Data;
using server.Data.Models;

namespace server.Endpoints;

public static class GroupEndpoints
{
    private readonly static int _maxGroupMembers = 2;

    public static void RegisterGroupEndpoints(this WebApplication app)
    {
        var endpoints = app.MapGroup("/api/groups").WithTags("Groups").RequireAuthorization();

        endpoints.MapGet("/", GetMyGroups).WithName("GetMyGroups");
        endpoints.MapPost("/", CreateGroup).WithName("CreateGroup");
        endpoints.MapGet("/{id:guid}", GetGroupDetails).WithName("GetGroupDetails");
        endpoints.MapPost("/{id:guid}/users", InviteUserToGroup).WithName("InviteUserToGroup");
        endpoints.MapPost("/{id:guid}/leave", LeaveGroup).WithName("LeaveGroup");
        endpoints.MapPost("/{id:guid}/reinvite", ReinviteUser).WithName("ReinviteUser");
    }

    internal static async Task<Results<Ok<List<GroupSummaryResponse>>, UnauthorizedHttpResult>> GetMyGroups(
        ClaimsPrincipal claimsUser,
        ApplicationDbContext db)
    {
        var userId = claimsUser.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return TypedResults.Unauthorized();

        var groups = await db.GroupUsers
            .Where(gu => gu.UserId == userId && gu.LeftAt == null) // Filter out groups the user left
            .Include(gu => gu.Group)
                .ThenInclude(g => g.GroupUsers)
                .ThenInclude(gu => gu.User)
            .OrderByDescending(gu => gu.Group.UpdatedAt)
            .Select(gu => new GroupSummaryResponse(
                gu.GroupId,
                gu.Group.GroupUsers.Select(m => new GroupMemberResponse(m.UserId, m.User.FirstName, m.User.LastName, m.User.Image, m.LeftAt != null, m.User.VenmoHandle, m.User.CashAppHandle, m.User.PayPalHandle)).ToList(),
                gu.Group.StreakCount,
                gu.Group.UpdatedAt,
                gu.AccumulatedPenaltyCents
            ))
            .ToListAsync();

        return TypedResults.Ok(groups);
    }

    internal static async Task<Results<Ok<GroupSummaryResponse>, UnauthorizedHttpResult>> CreateGroup(
        ClaimsPrincipal claimsUser,
        ApplicationDbContext db)
    {
        var userId = claimsUser.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return TypedResults.Unauthorized();

        var group = new Group();
        db.Groups.Add(group);

        var groupUser = new GroupUser
        {
            Group = group,
            UserId = userId
        };
        db.GroupUsers.Add(groupUser);

        await db.SaveChangesAsync();

        var currentUser = await db.Users.FindAsync(userId);

        var memberResponse = new GroupMemberResponse(userId, currentUser?.FirstName, currentUser?.LastName, currentUser?.Image, false);

        return TypedResults.Ok(new GroupSummaryResponse(
            group.Id,
            [memberResponse],
            group.StreakCount,
            group.UpdatedAt,
            0 // AccumulatedPenaltyCents is initially 0
        ));
    }

    internal static async Task<Results<Ok<GroupDetailsResponse>, NotFound<UserEndpoints.ErrorResponse>, UnauthorizedHttpResult>> GetGroupDetails(
        Guid id,
        ClaimsPrincipal claimsUser,
        ApplicationDbContext db)
    {
        var userId = claimsUser.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return TypedResults.Unauthorized();

        var group = await db.Groups
            .Include(g => g.GroupUsers)
                .ThenInclude(gu => gu.User)
            .FirstOrDefaultAsync(g => g.Id == id);

        if (group is null) return TypedResults.NotFound(new UserEndpoints.ErrorResponse("Group not found."));

        if (!group.GroupUsers.Any(gu => gu.UserId == userId && gu.LeftAt == null))
            return TypedResults.NotFound(new UserEndpoints.ErrorResponse("Group not found."));

        var members = group.GroupUsers.Select(m => new GroupMemberResponse(m.UserId, m.User.FirstName, m.User.LastName, m.User.Image, m.LeftAt != null, m.User.VenmoHandle, m.User.CashAppHandle, m.User.PayPalHandle)).ToList();

        return TypedResults.Ok(new GroupDetailsResponse(group.Id, members, group.StreakCount, group.CreatedAt, group.UpdatedAt));
    }

    internal static async Task<Results<Ok<InviteCreatedResponse>, BadRequest<UserEndpoints.ErrorResponse>, NotFound<UserEndpoints.ErrorResponse>, UnauthorizedHttpResult>> InviteUserToGroup(
        Guid id,
        InviteUserRequest request,
        ClaimsPrincipal claimsUser,
        ApplicationDbContext db,
        Services.CentrifugoService centrifugo)
    {
        var currentUserId = claimsUser.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(currentUserId)) return TypedResults.Unauthorized();

        var group = await db.Groups
            .Include(g => g.GroupUsers)
            .FirstOrDefaultAsync(g => g.Id == id);

        if (group is null) return TypedResults.NotFound(new UserEndpoints.ErrorResponse("Group not found."));

        if (!group.GroupUsers.Any(gu => gu.UserId == currentUserId && gu.LeftAt == null))
            return TypedResults.NotFound(new UserEndpoints.ErrorResponse("Group not found.")); 

        // If the group has a Left member, it technically has 2 GroupUser records, so it's "full". 
        // Thus, we shouldn't invite someone new. Capacity check automatically handles this.
        if (group.GroupUsers.Count >= _maxGroupMembers)
            return TypedResults.BadRequest(new UserEndpoints.ErrorResponse("Group is already at maximum capacity (2 members). If your partner left, invite them back instead."));

        var userToInvite = await db.Users.FirstOrDefaultAsync(u => u.FriendCode == request.FriendCode);
        if (userToInvite is null)
            return TypedResults.NotFound(new UserEndpoints.ErrorResponse("Invalid friend code. User not found."));

        if (userToInvite.Id == currentUserId)
            return TypedResults.BadRequest(new UserEndpoints.ErrorResponse("You cannot invite yourself."));

        if (group.GroupUsers.Any(gu => gu.UserId == userToInvite.Id))
            return TypedResults.BadRequest(new UserEndpoints.ErrorResponse("User is already in this group."));

        var existingInvite = await db.GroupInvites.AnyAsync(gi =>
            gi.GroupId == id && gi.InviteeId == userToInvite.Id && gi.Status == InviteStatus.Pending);
        if (existingInvite)
            return TypedResults.BadRequest(new UserEndpoints.ErrorResponse("An invite is already pending for this user."));

        var invite = new GroupInvite
        {
            GroupId = id,
            InviterId = currentUserId,
            InviteeId = userToInvite.Id,
            Status = InviteStatus.Pending
        };
        db.GroupInvites.Add(invite);

        await db.SaveChangesAsync();

        var currentUser = await db.Users.FindAsync(currentUserId);
        await centrifugo.PublishToUserAsync(userToInvite.Id, new
        {
            type = "InviteReceived",
            inviteId = invite.Id,
            groupId = id,
            inviterId = currentUserId,
            inviterName = $"{currentUser?.FirstName} {currentUser?.LastName}".Trim(),
            inviterImage = currentUser?.Image
        });

        return TypedResults.Ok(new InviteCreatedResponse(invite.Id));
    }

    internal static async Task<Results<NoContent, NotFound<UserEndpoints.ErrorResponse>, UnauthorizedHttpResult>> LeaveGroup(
        Guid id,
        ClaimsPrincipal claimsUser,
        ApplicationDbContext db,
        Services.CentrifugoService centrifugo)
    {
        var currentUserId = claimsUser.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(currentUserId)) return TypedResults.Unauthorized();

        var groupUser = await db.GroupUsers.FirstOrDefaultAsync(gu => gu.GroupId == id && gu.UserId == currentUserId);
        
        if (groupUser is null || groupUser.LeftAt != null)
            return TypedResults.NotFound(new UserEndpoints.ErrorResponse("Group not found or already left."));

        groupUser.LeftAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();

        var otherMember = await db.GroupUsers.FirstOrDefaultAsync(gu => gu.GroupId == id && gu.UserId != currentUserId);
        if (otherMember != null && otherMember.LeftAt == null)
        {
            await centrifugo.PublishToUserAsync(otherMember.UserId, new
            {
                type = "MemberLeft",
                groupId = id,
                userId = currentUserId
            });
        }

        return TypedResults.NoContent();
    }

    internal static async Task<Results<Ok<InviteCreatedResponse>, BadRequest<UserEndpoints.ErrorResponse>, NotFound<UserEndpoints.ErrorResponse>, UnauthorizedHttpResult>> ReinviteUser(
        Guid id,
        ClaimsPrincipal claimsUser,
        ApplicationDbContext db,
        Services.CentrifugoService centrifugo)
    {
        var currentUserId = claimsUser.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(currentUserId)) return TypedResults.Unauthorized();

        var group = await db.Groups
            .Include(g => g.GroupUsers)
            .FirstOrDefaultAsync(g => g.Id == id);

        if (group is null) return TypedResults.NotFound(new UserEndpoints.ErrorResponse("Group not found."));

        if (!group.GroupUsers.Any(gu => gu.UserId == currentUserId && gu.LeftAt == null))
            return TypedResults.NotFound(new UserEndpoints.ErrorResponse("Group not found.")); 

        var userWhoLeft = group.GroupUsers.FirstOrDefault(gu => gu.LeftAt != null);
        if (userWhoLeft is null)
            return TypedResults.BadRequest(new UserEndpoints.ErrorResponse("No one has left this group."));

        var existingInvite = await db.GroupInvites.AnyAsync(gi =>
            gi.GroupId == id && gi.InviteeId == userWhoLeft.UserId && gi.Status == InviteStatus.Pending);
        if (existingInvite)
            return TypedResults.BadRequest(new UserEndpoints.ErrorResponse("An invite is already pending for this user."));

        var invite = new GroupInvite
        {
            GroupId = id,
            InviterId = currentUserId,
            InviteeId = userWhoLeft.UserId,
            Status = InviteStatus.Pending
        };
        db.GroupInvites.Add(invite);

        await db.SaveChangesAsync();

        var currentUser = await db.Users.FindAsync(currentUserId);
        await centrifugo.PublishToUserAsync(userWhoLeft.UserId, new
        {
            type = "InviteReceived",
            inviteId = invite.Id,
            groupId = id,
            inviterId = currentUserId,
            inviterName = $"{currentUser?.FirstName} {currentUser?.LastName}".Trim(),
            inviterImage = currentUser?.Image
        });

        return TypedResults.Ok(new InviteCreatedResponse(invite.Id));
    }

    internal sealed record InviteUserRequest(string FriendCode);
    internal sealed record InviteCreatedResponse(Guid InviteId);
    internal sealed record GroupMemberResponse(string UserId, string? FirstName, string? LastName, string? Image, bool HasLeft, string? VenmoHandle = null, string? CashAppHandle = null, string? PayPalHandle = null);
    internal sealed record GroupSummaryResponse(Guid Id, List<GroupMemberResponse> Members, int StreakCount, DateTimeOffset UpdatedAt, int AccumulatedPenaltyCents);
    internal sealed record GroupDetailsResponse(Guid Id, List<GroupMemberResponse> Members, int StreakCount, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);
}
