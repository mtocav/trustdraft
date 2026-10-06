using Microsoft.EntityFrameworkCore;
using TrustDraft.Core.Abstractions;
using TrustDraft.Core.Domain;
using TrustDraft.Infrastructure.Persistence;

namespace TrustDraft.Api.Endpoints;

public record UpdateAnswerRequest(string? FinalText, YesNoPartial? Verdict);

public static class QuestionnaireEndpoints
{
    public static RouteGroupBuilder MapQuestionnaireEndpoints(this RouteGroupBuilder g)
    {
        g.MapGet("/", async (AppDbContext db) =>
            await db.Questionnaires
                .OrderByDescending(q => q.CreatedAt)
                .Select(q => new
                {
                    q.Id, q.Name, q.RequestedBy, q.Status, q.CreatedAt,
                    Total = q.Questions.Count,
                    Approved = q.Questions.Count(x => x.Answer != null && x.Answer.Status == AnswerStatus.Approved),
                })
                .ToListAsync());

        g.MapPost("/", async (IFormFile file, string? requestedBy, AppDbContext db, IFileStorage storage, ITenantContext tenant, CancellationToken ct) =>
        {
            if (!Path.GetExtension(file.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
                return Results.BadRequest(new { error = "MVP supports .xlsx questionnaires only." });

            await using var stream = file.OpenReadStream();
            var key = await storage.SaveAsync(tenant.TenantId, file.FileName, stream, ct);

            var q = new Questionnaire
            {
                TenantId = tenant.TenantId,
                Name = Path.GetFileNameWithoutExtension(file.FileName),
                RequestedBy = requestedBy,
                SourceFileName = Path.GetFileName(file.FileName),
                StorageKey = key,
            };
            db.Questionnaires.Add(q);
            db.Jobs.Add(new Job { TenantId = tenant.TenantId, Type = JobType.ImportQuestionnaire, TargetId = q.Id });
            await db.SaveChangesAsync(ct);

            return Results.Created($"/api/questionnaires/{q.Id}", new { q.Id, q.Name });
        }).DisableAntiforgery();

        g.MapGet("/{id:guid}", async (Guid id, AppDbContext db, CancellationToken ct) =>
        {
            var q = await db.Questionnaires
                .Where(x => x.Id == id)
                .Select(x => new
                {
                    x.Id, x.Name, x.Status,
                    Questions = x.Questions.OrderBy(qq => qq.Ordinal).Select(qq => new
                    {
                        qq.Id, qq.Ordinal, qq.Text,
                        Answer = qq.Answer == null ? null : new
                        {
                            qq.Answer.Id, qq.Answer.DraftText, qq.Answer.FinalText,
                            qq.Answer.Verdict, qq.Answer.Status,
                            qq.Answer.Confidence, qq.Answer.InsufficientInfo,
                            Citations = qq.Answer.Citations.Select(c => new { c.ChunkId, c.Chunk!.Locator, Document = c.Chunk.Document!.FileName }),
                        },
                    }),
                })
                .FirstOrDefaultAsync(ct);
            return q is null ? Results.NotFound() : Results.Ok(q);
        });

        g.MapPost("/{id:guid}/draft", async (Guid id, AppDbContext db, ITenantContext tenant, CancellationToken ct) =>
        {
            if (!await db.Questionnaires.AnyAsync(x => x.Id == id, ct)) return Results.NotFound();
            db.Jobs.Add(new Job { TenantId = tenant.TenantId, Type = JobType.DraftAnswers, TargetId = id });
            await db.SaveChangesAsync(ct);
            return Results.Accepted();
        });

        g.MapPut("/answers/{answerId:guid}", async (Guid answerId, UpdateAnswerRequest req, AppDbContext db, CancellationToken ct) =>
        {
            var a = await db.Answers.FirstOrDefaultAsync(x => x.Id == answerId, ct);
            if (a is null) return Results.NotFound();
            a.FinalText = req.FinalText;
            a.Verdict = req.Verdict ?? a.Verdict;
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });

        g.MapPost("/answers/{answerId:guid}/approve", async (Guid answerId, AppDbContext db, ITenantContext tenant, CancellationToken ct) =>
        {
            var a = await db.Answers.Include(x => x.Question).FirstOrDefaultAsync(x => x.Id == answerId, ct);
            if (a is null) return Results.NotFound();

            a.Status = AnswerStatus.Approved;
            a.FinalText ??= a.DraftText;

            // Feed the answer library. TODO (week 6): embed QuestionText so future questions can reuse it.
            db.LibraryEntries.Add(new LibraryEntry
            {
                TenantId = tenant.TenantId,
                QuestionText = a.Question!.Text,
                AnswerText = a.FinalText ?? "",
                Verdict = a.Verdict,
                SourceAnswerId = a.Id,
            });
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });

        // TODO (week 7): GET /{id}/export -> fill answers back into the original .xlsx (ClosedXML) and stream it.

        return g;
    }
}
