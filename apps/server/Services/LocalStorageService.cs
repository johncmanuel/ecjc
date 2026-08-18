namespace server.Services;

public class LocalStorageService : IStorageService
{
    private readonly string _uploadsPath;

    public LocalStorageService(IWebHostEnvironment env)
    {
        _uploadsPath = Path.Combine(env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot"), "uploads");
        Directory.CreateDirectory(_uploadsPath);
    }

    public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(fileName);
        var uniqueName = $"{Guid.NewGuid()}{extension}";
        var fullPath = Path.Combine(_uploadsPath, uniqueName);

        await using var outputStream = new FileStream(fullPath, FileMode.Create);
        await fileStream.CopyToAsync(outputStream, cancellationToken);

        return $"/uploads/{uniqueName}";
    }

    public Task DeleteFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        // the file path is a relative URL like "/uploads/abc.jpg"
        var fileName = Path.GetFileName(filePath);
        var fullPath = Path.Combine(_uploadsPath, fileName);

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }

    public Task<Stream?> GetFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        // the file path is a relative URL like "/uploads/abc.jpg"
        var fileName = Path.GetFileName(filePath);
        var fullPath = Path.Combine(_uploadsPath, fileName);

        if (!File.Exists(fullPath))
        {
            return Task.FromResult<Stream?>(null);
        }

        Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
        return Task.FromResult<Stream?>(stream);
    }
}
