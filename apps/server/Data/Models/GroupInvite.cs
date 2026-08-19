namespace server.Data.Models;

public enum InviteStatus
{
    Pending,
    Accepted,
    Declined
}

public class GroupInvite
{
    public Guid Id { get; set; }
    public Guid GroupId { get; set; }
    public string InviterId { get; set; } = string.Empty;
    public string InviteeId { get; set; } = string.Empty;
    public InviteStatus Status { get; set; } = InviteStatus.Pending;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    // Navigation properties
    public Group Group { get; set; } = null!;
    public User Inviter { get; set; } = null!;
    public User Invitee { get; set; } = null!;
}
