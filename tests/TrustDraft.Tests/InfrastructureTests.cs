using System.Text;
using Microsoft.Extensions.Options;
using TrustDraft.Core.Abstractions;
using TrustDraft.Infrastructure.Ai;
using TrustDraft.Infrastructure.Storage;

namespace TrustDraft.Tests;

public class StubAnswerGeneratorTests
{
    [Fact]
    public async Task Returns_insufficient_info_when_there_is_no_context()
    {
        var result = await new StubAnswerGenerator().DraftAsync("Do you encrypt data at rest?", []);

        Assert.True(result.InsufficientInfo);
        Assert.Empty(result.CitationChunkIds);
    }

    [Fact]
    public async Task Cites_the_top_chunk_when_context_exists()
    {
        var chunk = new RetrievedChunk(Guid.NewGuid(), "All laptops use BitLocker.", "p. 2", "Security Policy.pdf", 0.9);

        var result = await new StubAnswerGenerator().DraftAsync("Do you encrypt laptops?", [chunk]);

        Assert.False(result.InsufficientInfo);
        Assert.Equal(new[] { chunk.ChunkId }, result.CitationChunkIds);
    }
}

public class LocalFileStorageTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "trustdraft-tests-" + Guid.NewGuid().ToString("N"));
    private LocalFileStorage Storage => new(Options.Create(new StorageOptions { RootPath = _root }));

    [Fact]
    public async Task Saves_and_reads_back_a_file_scoped_to_the_tenant()
    {
        var tenant = Guid.NewGuid();
        var key = await Storage.SaveAsync(tenant, "policy.md", new MemoryStream(Encoding.UTF8.GetBytes("hello")));

        Assert.StartsWith(tenant.ToString("N"), key);
        await using var s = await Storage.OpenReadAsync(key);
        Assert.Equal("hello", await new StreamReader(s).ReadToEndAsync());
    }

    [Fact]
    public async Task Strips_directory_traversal_from_file_names()
    {
        var key = await Storage.SaveAsync(Guid.NewGuid(), "../../evil.txt", new MemoryStream([1]));

        Assert.EndsWith("/evil.txt", key);
        Assert.DoesNotContain("..", key);
    }

    [Fact]
    public async Task Rejects_storage_keys_outside_the_root()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => Storage.OpenReadAsync("../../etc/passwd"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }
}
