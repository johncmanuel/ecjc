namespace server.Data.Models;

public enum MediaType
{
    Image,
    Video,
    Gif,
    Other
}

public class MediaAttachment
{
    public Guid Id { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public MediaType MediaType { get; set; }
    public long FileSize { get; set; }
    public DateTimeOffset UploadedAt { get; set; }

    public Guid EntryId { get; set; }
    public Entry Entry { get; set; } = null!;
}
