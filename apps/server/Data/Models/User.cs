namespace server.Data.Models;

public class User
{
    // id, email, name, and image are provided by the authentication provider (e.g., Google, GitHub, etc.)
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string FriendCode { get; set; } = string.Empty;
    public string? Image { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    // Navigation properties
    public ICollection<GroupUser> GroupUsers { get; set; } = [];
    public ICollection<Entry> Entries { get; set; } = [];
    public ICollection<Reaction> Reactions { get; set; } = [];
}
