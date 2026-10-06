using Pgvector;

namespace TrustDraft.Core.Domain;

/// <summary>Base for everything that belongs to a single customer (tenant).</summary>
public abstract class TenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class Tenant
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Name { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class User : TenantEntity
{
    public required string Email { get; set; }
    public string? DisplayName { get; set; }
}

// ---------- Knowledge base ----------

public enum DocumentKind { Policy, PastQuestionnaire, Other }
public enum ProcessingStatus { Pending, Processing, Ready, Failed }

public class Document : TenantEntity
{
    public required string FileName { get; set; }
    public required string ContentType { get; set; }
    public required string StorageKey { get; set; }
    public DocumentKind Kind { get; set; } = DocumentKind.Policy;
    public ProcessingStatus Status { get; set; } = ProcessingStatus.Pending;
    public string? Error { get; set; }
    public List<Chunk> Chunks { get; set; } = [];
}

public class Chunk : TenantEntity
{
    public Guid DocumentId { get; set; }
    public Document? Document { get; set; }
    public int Ordinal { get; set; }
    /// <summary>Human-readable location, e.g. "p. 4" or "Sheet1!B12".</summary>
    public string? Locator { get; set; }
    public required string Content { get; set; }
    public Vector? Embedding { get; set; }
}

// ---------- Questionnaires ----------

public class Questionnaire : TenantEntity
{
    public required string Name { get; set; }
    public string? RequestedBy { get; set; }
    public required string SourceFileName { get; set; }
    public required string StorageKey { get; set; }
    public ProcessingStatus Status { get; set; } = ProcessingStatus.Pending;
    /// <summary>Detected/confirmed column mapping, stored as JSON (sheet, question column, answer column...).</summary>
    public string? ColumnMappingJson { get; set; }
    public List<Question> Questions { get; set; } = [];
}

public class Question : TenantEntity
{
    public Guid QuestionnaireId { get; set; }
    public Questionnaire? Questionnaire { get; set; }
    public int Ordinal { get; set; }
    public required string Text { get; set; }
    /// <summary>Where the answer goes back on export, e.g. "Sheet1!C14".</summary>
    public string? AnswerCellRef { get; set; }
    public Answer? Answer { get; set; }
}

public enum AnswerStatus { Draft, NeedsReview, Approved }
public enum YesNoPartial { Yes, No, Partial, NotApplicable }

public class Answer : TenantEntity
{
    public Guid QuestionId { get; set; }
    public Question? Question { get; set; }
    public string? DraftText { get; set; }
    public string? FinalText { get; set; }
    public YesNoPartial? Verdict { get; set; }
    public double Confidence { get; set; }
    public bool InsufficientInfo { get; set; }
    /// <summary>Set when the answer was reused from the approved answer library.</summary>
    public Guid? ReusedFromLibraryEntryId { get; set; }
    public AnswerStatus Status { get; set; } = AnswerStatus.Draft;
    public List<AnswerCitation> Citations { get; set; } = [];
}

public class AnswerCitation
{
    public Guid AnswerId { get; set; }
    public Guid ChunkId { get; set; }
    public Chunk? Chunk { get; set; }
}

/// <summary>Approved Q/A pairs. Every approved answer lands here, so each questionnaire makes the next one faster.</summary>
public class LibraryEntry : TenantEntity
{
    public required string QuestionText { get; set; }
    public required string AnswerText { get; set; }
    public YesNoPartial? Verdict { get; set; }
    public Vector? Embedding { get; set; }
    public Guid? SourceAnswerId { get; set; }
}

// ---------- Background jobs ----------

public enum JobType { ProcessDocument, ImportQuestionnaire, DraftAnswers, ExportQuestionnaire }
public enum JobStatus { Queued, Running, Succeeded, Failed }

public class Job
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public JobType Type { get; set; }
    public Guid TargetId { get; set; }
    public JobStatus Status { get; set; } = JobStatus.Queued;
    public int Attempts { get; set; }
    public string? Error { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? FinishedAt { get; set; }
}

// ---------- Audit ----------

public class AuditEvent : TenantEntity
{
    public Guid? UserId { get; set; }
    public required string Action { get; set; }
    public string? TargetType { get; set; }
    public Guid? TargetId { get; set; }
    public string? DataJson { get; set; }
}
