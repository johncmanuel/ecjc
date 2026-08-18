using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using server.Data;
using server.Data.Models;

namespace server.Endpoints;

public static class GroupEndpoints
{
    public static void RegisterGroupEndpoints(this WebApplication app)
    {
        var endpoints = app.MapGroup("/api/groups").WithTags("Groups").RequireAuthorization();

        endpoints.MapGet("/", GetMyGroups).WithName("GetMyGroups");
        endpoints.MapPost("/", CreateGroup).WithName("CreateGroup");
        endpoints.MapGet("/{id:guid}", GetGroupDetails).WithName("GetGroupDetails");
        endpoints.MapPost("/{id:guid}/users", InviteUserToGroup).WithName("InviteUserToGroup");
    }

    internal static async Task<Results<Ok<List<GroupSummaryResponse>>, UnauthorizedHttpResult>> GetMyGroups(
        ClaimsPrincipal claimsUser,
        ApplicationDbContext db)
    {
        var userId = claimsUser.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return TypedResults.Unauthorized();

        var groups = await db.GroupUsers
            .Where(gu => gu.UserId == userId)
            .Include(gu => gu.Group)
                .ThenInclude(g => g.GroupUsers)
                .ThenInclude(gu => gu.User)
            .OrderByDescending(gu => gu.Group.UpdatedAt)
            .Select(gu => new GroupSummaryResponse(
                gu.GroupId,
                gu.Group.GroupUsers.Select(m => new GroupMemberResponse(m.UserId, m.User.FirstName, m.User.LastName, m.User.Image)).ToList(),
                gu.Group.StreakCount,
                gu.Group.UpdatedAt
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
            GroupId = group.Id,
            UserId = userId
        };
        db.GroupUsers.Add(groupUser);

        await db.SaveChangesAsync();

        var currentUser = await db.Users.FindAsync(userId);

        var memberResponse = new GroupMemberResponse(userId, currentUser?.FirstName, currentUser?.LastName, currentUser?.Image);

        return TypedResults.Ok(new GroupSummaryResponse(
            group.Id,
            [memberResponse],
            group.StreakCount,
            group.UpdatedAt
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

        if (!group.GroupUsers.Any(gu => gu.UserId == userId))
            return TypedResults.NotFound(new UserEndpoints.ErrorResponse("Group not found."));

        var members = group.GroupUsers.Select(m => new GroupMemberResponse(m.UserId, m.User.FirstName, m.User.LastName, m.User.Image)).ToList();

        return TypedResults.Ok(new GroupDetailsResponse(group.Id, members, group.StreakCount, group.CreatedAt, group.UpdatedAt));
    }

    internal static async Task<Results<Ok<GroupDetailsResponse>, BadRequest<UserEndpoints.ErrorResponse>, NotFound<UserEndpoints.ErrorResponse>, UnauthorizedHttpResult>> InviteUserToGroup(
        Guid id,
        InviteUserRequest request,
        ClaimsPrincipal claimsUser,
        ApplicationDbContext db)
    {
        var currentUserId = claimsUser.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(currentUserId)) return TypedResults.Unauthorized();

        var group = await db.Groups
            .Include(g => g.GroupUsers)
            .FirstOrDefaultAsync(g => g.Id == id);

        if (group is null) return TypedResults.NotFound(new UserEndpoints.ErrorResponse("Group not found."));

        if (!group.GroupUsers.Any(gu => gu.UserId == currentUserId))
            return TypedResults.NotFound(new UserEndpoints.ErrorResponse("Group not found.")); 

        if (group.GroupUsers.Count >= 2)
            return TypedResults.BadRequest(new UserEndpoints.ErrorResponse("Group is already at maximum capacity (2 members)."));

        var userToInvite = await db.Users.FirstOrDefaultAsync(u => u.FriendCode == request.FriendCode);
        if (userToInvite is null)
            return TypedResults.NotFound(new UserEndpoints.ErrorResponse("Invalid friend code. User not found."));

        if (group.GroupUsers.Any(gu => gu.UserId == userToInvite.Id))
            return TypedResults.BadRequest(new UserEndpoints.ErrorResponse("User is already in this group."));

        var newMember = new GroupUser
        {
            GroupId = group.Id,
            UserId = userToInvite.Id
        };
        db.GroupUsers.Add(newMember);

        await db.SaveChangesAsync();

        // Reload to get updated members
        var updatedGroup = await db.Groups
            .Include(g => g.GroupUsers)
                .ThenInclude(gu => gu.User)
            .FirstAsync(g => g.Id == id);

        var members = updatedGroup.GroupUsers.Select(m => new GroupMemberResponse(m.UserId, m.User.FirstName, m.User.LastName, m.User.Image)).ToList();

        return TypedResults.Ok(new GroupDetailsResponse(updatedGroup.Id, members, updatedGroup.StreakCount, updatedGroup.CreatedAt, updatedGroup.UpdatedAt));
    }

    internal sealed record InviteUserRequest(string FriendCode);
    internal sealed record GroupMemberResponse(string UserId, string? FirstName, string? LastName, string? Image);
    internal sealed record GroupSummaryResponse(Guid Id, List<GroupMemberResponse> Members, int StreakCount, DateTimeOffset UpdatedAt);
    internal sealed record GroupDetailsResponse(Guid Id, List<GroupMemberResponse> Members, int StreakCount, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);
}
