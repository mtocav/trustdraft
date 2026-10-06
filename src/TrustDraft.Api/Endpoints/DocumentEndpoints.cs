using Microsoft.EntityFrameworkCore;
using TrustDraft.Core.Abstractions;
using TrustDraft.Core.Domain;
using TrustDraft.Infrastructure.Persistence;

namespace TrustDraft.Api.Endpoints;

public static class DocumentEndpoints
{
    private static readonly string[] AllowedExtensions = [".pdf", ".docx", ".md", ".txt", ".xlsx"];
    private const long MaxBytes = 25 * 1024 * 1024;

    public static RouteGroupBuilder MapDocumentEndpoints(this RouteGroupBuilder g)
    {
        g.MapGet("/", async (AppDbContext db) =>
            await db.Documents
                .OrderByDescending(d => d.CreatedAt)
                .Select(d => new { d.Id, d.FileName, d.Kind, d.Status, d.CreatedAt, ChunkCount = d.Chunks.Count })
                .ToListAsync());

        g.MapPost("/", async (IFormFile file, DocumentKind? kind, AppDbContext db, IFileStorage storage, ITenantContext tenant, CancellationToken ct) =>
        {
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(ext))
                return Results.BadRequest(new { error = $"Unsupported file type '{ext}'." });
            if (file.Length is 0 or > MaxBytes)
                return Results.BadRequest(new { error = "File is empty or larger than 25 MB." });

            await using var stream = file.OpenReadStream();
            var key = await storage.SaveAsync(tenant.TenantId, file.FileName, stream, ct);

            var doc = new Document
            {
                TenantId = tenant.TenantId,
                FileName = Path.GetFileName(file.FileName),
                ContentType = file.ContentType,
                StorageKey = key,
                Kind = kind ?? (ext == ".xlsx" ? DocumentKind.PastQuestionnaire : DocumentKind.Policy),
            };
            db.Documents.Add(doc);
            db.Jobs.Add(new Job { TenantId = tenant.TenantId, Type = JobType.ProcessDocument, TargetId = doc.Id });
            await db.SaveChangesAsync(ct);

            return Results.Created($"/api/documents/{doc.Id}", new { doc.Id, doc.FileName, doc.Status });
        }).DisableAntiforgery(); // TODO: re-enable once real auth (cookies) is in place.

        g.MapDelete("/{id:guid}", async (Guid id, AppDbContext db, IFileStorage storage, CancellationToken ct) =>
        {
            var doc = await db.Documents.FirstOrDefaultAsync(d => d.Id == id, ct);
            if (doc is null) return Results.NotFound();
            await storage.DeleteAsync(doc.StorageKey, ct);
            db.Documents.Remove(doc);
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });

        return g;
    }
}
