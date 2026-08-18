namespace server.Data.Models;

public class Reaction
{
    public Guid Id { get; set; }
    public string EmojiCode { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }

    public Guid EntryId { get; set; }
    public Entry Entry { get; set; } = null!;

    public string UserId { get; set; } = string.Empty;
    public User User { get; set; } = null!;
}
