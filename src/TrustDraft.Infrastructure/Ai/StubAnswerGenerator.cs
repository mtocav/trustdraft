using TrustDraft.Core.Abstractions;

namespace TrustDraft.Infrastructure.Ai;

/// <summary>
/// Placeholder until week 5. Replace with a real provider (Claude / OpenAI / Azure OpenAI in an EU region)
/// that returns structured JSON: { answer, verdict, citations[], confidence, insufficient_info }.
/// Rule that must survive the swap: if the context doesn't support an answer, return InsufficientInfo = true.
/// </summary>
public class StubAnswerGenerator : IAnswerGenerator
{
    public Task<DraftedAnswer> DraftAsync(string question, IReadOnlyList<RetrievedChunk> context, CancellationToken ct = default)
    {
        if (context.Count == 0)
            return Task.FromResult(new DraftedAnswer("Insufficient information in the knowledge base.", null, [], 0, true));

        var top = context[0];
        return Task.FromResult(new DraftedAnswer(
            $"[stub] Based on {top.DocumentName}: {Truncate(top.Content, 200)}",
            null,
            [top.ChunkId],
            0.1,
            false));
    }

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max] + "…";
}
