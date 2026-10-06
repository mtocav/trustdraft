using Microsoft.Extensions.Options;
using TrustDraft.Core.Abstractions;

namespace TrustDraft.Infrastructure.Storage;

public class StorageOptions
{
    public string RootPath { get; set; } = "data/files";
}

/// <summary>Dev storage on local disk. Swap for an S3-compatible (EU region) implementation before going live.</summary>
public class LocalFileStorage(IOptions<StorageOptions> options) : IFileStorage
{
    // Trailing separator so "/data/files-evil" can't pass the StartsWith check for "/data/files".
    private readonly string _root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(options.Value.RootPath)) + Path.DirectorySeparatorChar;

    public async Task<string> SaveAsync(Guid tenantId, string fileName, Stream content, CancellationToken ct = default)
    {
        var safeName = Path.GetFileName(fileName);
        var key = $"{tenantId:N}/{Guid.NewGuid():N}/{safeName}";
        var path = Resolve(key);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await using var file = File.Create(path);
        await content.CopyToAsync(file, ct);
        return key;
    }

    public Task<Stream> OpenReadAsync(string storageKey, CancellationToken ct = default) =>
        Task.FromResult<Stream>(File.OpenRead(Resolve(storageKey)));

    public Task DeleteAsync(string storageKey, CancellationToken ct = default)
    {
        var path = Resolve(storageKey);
        if (File.Exists(path)) File.Delete(path);
        return Task.CompletedTask;
    }

    private string Resolve(string key)
    {
        var full = Path.GetFullPath(Path.Combine(_root, key));
        if (!full.StartsWith(_root, StringComparison.Ordinal))
            throw new InvalidOperationException("Invalid storage key.");
        return full;
    }
}
