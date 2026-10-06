using Microsoft.EntityFrameworkCore;
using TrustDraft.Core.Abstractions;
using TrustDraft.Core.Domain;
using TrustDraft.Infrastructure.Persistence;

namespace TrustDraft.Api;

/// <summary>
/// DEV ONLY: tenant comes from the X-Tenant-Id header, falling back to the seeded dev tenant.
/// TODO (week 1): replace with a claim from real auth (magic link / OIDC) and never trust a client header.
/// </summary>
public class HttpTenantContext(IHttpContextAccessor http) : ITenantContext
{
    public Guid TenantId =>
        Guid.TryParse(http.HttpContext?.Request.Headers["X-Tenant-Id"].ToString(), out var id) ? id : DevSeed.DevTenantId;
}

public static class DevSeed
{
    public static readonly Guid DevTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public static async Task RunAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        // Use real migrations once you've added them (`dotnet ef migrations add Initial`); until then just create the schema.
        if (db.Database.GetMigrations().Any())
            await db.Database.MigrateAsync();
        else
            await db.Database.EnsureCreatedAsync();

        if (!await db.Tenants.AnyAsync(t => t.Id == DevTenantId))
        {
            db.Tenants.Add(new Tenant { Id = DevTenantId, Name = "Acme IT B.V. (demo)" });
            await db.SaveChangesAsync();
        }
    }
}
