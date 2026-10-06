using TrustDraft.Core.Domain;

namespace TrustDraft.Core.Abstractions;

/// <summary>Resolves the current tenant (from the auth token in the API, from the job in the worker).</summary>
public interface ITenantContext
{
    Guid TenantId { get; }
}

public interface IFileStorage
{
    Task<string> SaveAsync(Guid tenantId, string fileName, Stream content, CancellationToken ct = default);
    Task<Stream> OpenReadAsync(string storageKey, CancellationToken ct = default);
    Task DeleteAsync(string storageKey, CancellationToken ct = default);
}

public record ParsedChunk(string Content, string? Locator);

/// <summary>Turns an uploaded file (pdf/docx/md/xlsx) into text chunks.</summary>
public interface IDocumentParser
{
    bool CanParse(string fileName, string contentType);
    IAsyncEnumerable<ParsedChunk> ParseAsync(Stream content, string fileName, CancellationToken ct = default);
}

public interface IEmbeddingClient
{
    int Dimensions { get; }
    Task<float[][]> EmbedAsync(IReadOnlyList<string> inputs, CancellationToken ct = default);
}

public record RetrievedChunk(Guid ChunkId, string Content, string? Locator, string DocumentName, double Score);

/// <summary>Hybrid (vector + full-text) search over a tenant's knowledge base.</summary>
public interface IRetriever
{
    Task<IReadOnlyList<RetrievedChunk>> SearchAsync(Guid tenantId, string query, int topK = 8, CancellationToken ct = default);
}

public record DraftedAnswer(
    string Answer,
    YesNoPartial? Verdict,
    IReadOnlyList<Guid> CitationChunkIds,
    double Confidence,
    bool InsufficientInfo);

/// <summary>Provider-agnostic LLM call that turns a question + context into a structured answer.</summary>
public interface IAnswerGenerator
{
    Task<DraftedAnswer> DraftAsync(string question, IReadOnlyList<RetrievedChunk> context, CancellationToken ct = default);
}
