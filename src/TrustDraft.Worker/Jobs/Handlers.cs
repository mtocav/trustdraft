using Microsoft.EntityFrameworkCore;
using TrustDraft.Core.Abstractions;
using TrustDraft.Core.Domain;
using TrustDraft.Infrastructure.Persistence;

namespace TrustDraft.Worker.Jobs;

/// <summary>Week 2: parse -> chunk -> embed -> store Chunks.</summary>
public class ProcessDocumentHandler(AppDbContext db, ILogger<ProcessDocumentHandler> log) : IJobHandler
{
    public JobType Type => JobType.ProcessDocument;

    public async Task HandleAsync(Job job, CancellationToken ct)
    {
        var doc = await db.Documents.FirstAsync(d => d.Id == job.TargetId, ct);
        doc.Status = ProcessingStatus.Processing;
        await db.SaveChangesAsync(ct);

        // TODO (week 2):
        //  1. pick an IDocumentParser by extension (PdfPig / OpenXML / ClosedXML / plain text)
        //  2. chunk ~500–800 tokens with overlap; keep a Locator ("p. 4", "§3.2", "Sheet1!B12")
        //  3. embed in batches with IEmbeddingClient and save Chunk rows
        log.LogWarning("ProcessDocument not implemented yet for {File}", doc.FileName);

        doc.Status = ProcessingStatus.Ready;
        await db.SaveChangesAsync(ct);
    }
}

/// <summary>Week 4: read the .xlsx, detect question/answer columns, create Question rows.</summary>
public class ImportQuestionnaireHandler(AppDbContext db, ILogger<ImportQuestionnaireHandler> log) : IJobHandler
{
    public JobType Type => JobType.ImportQuestionnaire;

    public async Task HandleAsync(Job job, CancellationToken ct)
    {
        var q = await db.Questionnaires.FirstAsync(x => x.Id == job.TargetId, ct);

        // TODO (week 4): ClosedXML -> find the header row, guess question + answer columns
        // (header keywords in NL/EN: "vraag/question", "antwoord/answer/response"), store in ColumnMappingJson,
        // and create one Question per non-empty row with AnswerCellRef set.
        log.LogWarning("ImportQuestionnaire not implemented yet for {File}", q.SourceFileName);

        q.Status = ProcessingStatus.Ready;
        await db.SaveChangesAsync(ct);
    }
}

/// <summary>Week 5: the answer pipeline — library match, hybrid retrieval, structured generation, validation.</summary>
public class DraftAnswersHandler(AppDbContext db, IAnswerGenerator generator, IServiceProvider sp) : IJobHandler
{
    public JobType Type => JobType.DraftAnswers;

    public async Task HandleAsync(Job job, CancellationToken ct)
    {
        var questions = await db.Questions
            .Include(x => x.Answer)
            .Where(x => x.QuestionnaireId == job.TargetId && (x.Answer == null || x.Answer.Status != AnswerStatus.Approved))
            .OrderBy(x => x.Ordinal)
            .ToListAsync(ct);

        var retriever = sp.GetService<IRetriever>(); // null until week 3

        foreach (var question in questions)
        {
            // TODO (week 6): 1. look for a near-duplicate approved LibraryEntry first and reuse it.
            IReadOnlyList<RetrievedChunk> context = retriever is null
                ? Array.Empty<RetrievedChunk>()
                : await retriever.SearchAsync(job.TenantId, question.Text, 8, ct);

            var draft = await generator.DraftAsync(question.Text, context, ct);

            // Only keep citations that point at chunks we actually retrieved (no invented sources).
            var retrievedIds = context.Select(c => c.ChunkId).ToHashSet();
            var citations = draft.CitationChunkIds.Where(retrievedIds.Contains).Distinct().ToList();

            var answer = question.Answer ?? new Answer { TenantId = job.TenantId, QuestionId = question.Id };
            answer.DraftText = draft.Answer;
            answer.Verdict = draft.Verdict;
            answer.Confidence = draft.Confidence;
            answer.InsufficientInfo = draft.InsufficientInfo;
            answer.Status = draft.InsufficientInfo || draft.Confidence < 0.6 ? AnswerStatus.NeedsReview : AnswerStatus.Draft;
            answer.Citations = citations.Select(id => new AnswerCitation { ChunkId = id }).ToList();

            if (question.Answer is null) db.Answers.Add(answer);
        }

        await db.SaveChangesAsync(ct);
    }
}
