namespace server.Data.Models;

public class GroupUser
{
    public string UserId { get; set; } = string.Empty;
    public User User { get; set; } = null!;

    public Guid GroupId { get; set; }
    public Group Group { get; set; } = null!;

    public DateTimeOffset JoinedAt { get; set; }
}
