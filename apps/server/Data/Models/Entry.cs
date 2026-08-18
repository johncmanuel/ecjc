namespace server.Data.Models;

public class Entry
{
    public Guid Id { get; set; }
    public string TextContent { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public string AuthorId { get; set; } = string.Empty;
    public User Author { get; set; } = null!;

    public Guid GroupId { get; set; }
    public Group Group { get; set; } = null!;

    // Navigation properties
    public ICollection<MediaAttachment> MediaAttachments { get; set; } = [];
    public ICollection<Reaction> Reactions { get; set; } = [];
}
