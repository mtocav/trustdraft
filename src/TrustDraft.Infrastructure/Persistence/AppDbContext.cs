using Microsoft.EntityFrameworkCore;
using TrustDraft.Core.Abstractions;
using TrustDraft.Core.Domain;

namespace TrustDraft.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options, ITenantContext tenant) : DbContext(options)
{
    /// <summary>Embedding size. Must match the embedding model you pick (1536 = OpenAI text-embedding-3-small).</summary>
    public const int EmbeddingDimensions = 1536;

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<Chunk> Chunks => Set<Chunk>();
    public DbSet<Questionnaire> Questionnaires => Set<Questionnaire>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<Answer> Answers => Set<Answer>();
    public DbSet<AnswerCitation> AnswerCitations => Set<AnswerCitation>();
    public DbSet<LibraryEntry> LibraryEntries => Set<LibraryEntry>();
    public DbSet<Job> Jobs => Set<Job>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

    // Read per query, so the filter always uses the current request's tenant.
    private Guid CurrentTenantId => tenant.TenantId;

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.HasPostgresExtension("vector");

        // Tenant isolation at the ORM level. TODO (week 1): add Postgres row-level security as a second layer.
        b.Entity<User>().HasQueryFilter(e => e.TenantId == CurrentTenantId);
        b.Entity<Document>().HasQueryFilter(e => e.TenantId == CurrentTenantId);
        b.Entity<Chunk>().HasQueryFilter(e => e.TenantId == CurrentTenantId);
        b.Entity<Questionnaire>().HasQueryFilter(e => e.TenantId == CurrentTenantId);
        b.Entity<Question>().HasQueryFilter(e => e.TenantId == CurrentTenantId);
        b.Entity<Answer>().HasQueryFilter(e => e.TenantId == CurrentTenantId);
        b.Entity<LibraryEntry>().HasQueryFilter(e => e.TenantId == CurrentTenantId);
        b.Entity<AuditEvent>().HasQueryFilter(e => e.TenantId == CurrentTenantId);

        b.Entity<User>().HasIndex(e => new { e.TenantId, e.Email }).IsUnique();

        b.Entity<Document>(e =>
        {
            e.Property(x => x.Kind).HasConversion<string>();
            e.Property(x => x.Status).HasConversion<string>();
            e.HasMany(x => x.Chunks).WithOne(x => x.Document).HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Chunk>(e =>
        {
            e.Property(x => x.Embedding).HasColumnType($"vector({EmbeddingDimensions})");
            e.HasIndex(x => x.Embedding).HasMethod("hnsw").HasOperators("vector_cosine_ops");
            e.HasIndex(x => new { x.TenantId, x.DocumentId });
            // TODO (week 2): add a generated tsvector column + GIN index for the keyword half of hybrid search.
        });

        b.Entity<Questionnaire>(e =>
        {
            e.Property(x => x.Status).HasConversion<string>();
            e.Property(x => x.ColumnMappingJson).HasColumnType("jsonb");
            e.HasMany(x => x.Questions).WithOne(x => x.Questionnaire).HasForeignKey(x => x.QuestionnaireId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Question>(e =>
        {
            e.HasOne(x => x.Answer).WithOne(x => x.Question).HasForeignKey<Answer>(x => x.QuestionId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Answer>(e =>
        {
            e.Property(x => x.Status).HasConversion<string>();
            e.Property(x => x.Verdict).HasConversion<string>();
            e.HasMany(x => x.Citations).WithOne().HasForeignKey(x => x.AnswerId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<AnswerCitation>(e =>
        {
            e.HasKey(x => new { x.AnswerId, x.ChunkId });
            e.HasOne(x => x.Chunk).WithMany().HasForeignKey(x => x.ChunkId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<LibraryEntry>(e =>
        {
            e.Property(x => x.Verdict).HasConversion<string>();
            e.Property(x => x.Embedding).HasColumnType($"vector({EmbeddingDimensions})");
            e.HasIndex(x => x.Embedding).HasMethod("hnsw").HasOperators("vector_cosine_ops");
        });

        b.Entity<Job>(e =>
        {
            e.Property(x => x.Type).HasConversion<string>();
            e.Property(x => x.Status).HasConversion<string>();
            e.HasIndex(x => new { x.Status, x.CreatedAt });
        });

        b.Entity<AuditEvent>(e => e.Property(x => x.DataJson).HasColumnType("jsonb"));
    }
}
