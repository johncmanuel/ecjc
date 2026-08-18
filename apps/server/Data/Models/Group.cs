namespace server.Data.Models;

public class Group
{
    public Guid Id { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public int StreakCount { get; set; }

    // Navigation properties
    public ICollection<GroupUser> GroupUsers { get; set; } = [];
    public ICollection<Entry> Entries { get; set; } = [];
}
