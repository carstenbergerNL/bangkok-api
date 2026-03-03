using Bangkok.Application.Configuration;
using Bangkok.Application.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Bangkok.Infrastructure.Services;

public class LocalAttachmentFileStorage : IAttachmentFileStorage
{
    private readonly string _rootPath;
    private readonly IOptions<AttachmentSettings> _options;

    public LocalAttachmentFileStorage(IHostEnvironment env, IOptions<AttachmentSettings> options)
    {
        _options = options;
        _rootPath = Path.Combine(env.ContentRootPath ?? AppContext.BaseDirectory, "uploads");
    }

    public async Task<string> SaveAsync(Guid taskId, string fileName, Stream content, CancellationToken cancellationToken = default)
    {
        var safeName = SanitizeFileName(fileName);
        var relativeDir = Path.Combine("tasks", taskId.ToString());
        var fullDir = Path.Combine(_rootPath, relativeDir);
        Directory.CreateDirectory(fullDir);
        var uniqueName = $"{Guid.NewGuid():N}_{safeName}";
        var relativePath = Path.Combine(relativeDir, uniqueName).Replace('\\', '/');
        var fullPath = Path.Combine(_rootPath, relativeDir, uniqueName);
        await using (var fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true))
        {
            await content.CopyToAsync(fs, cancellationToken).ConfigureAwait(false);
        }
        return relativePath;
    }

    public Task DeleteAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(relativePath)) return Task.CompletedTask;
        var fullPath = Path.Combine(_rootPath, relativePath.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(fullPath))
            File.Delete(fullPath);
        return Task.CompletedTask;
    }

    public Task<Stream?> GetStreamAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(relativePath)) return Task.FromResult<Stream?>(null);
        var fullPath = Path.Combine(_rootPath, relativePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(fullPath)) return Task.FromResult<Stream?>(null);
        return Task.FromResult<Stream?>(new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read));
    }

    private static string SanitizeFileName(string fileName)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var name = Path.GetFileName(fileName);
        if (string.IsNullOrEmpty(name)) name = "file";
        foreach (var c in invalid)
            name = name.Replace(c, '_');
        return name.Length > 200 ? name[..200] : name;
    }
}
