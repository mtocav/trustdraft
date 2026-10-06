using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Pgvector.EntityFrameworkCore;
using TrustDraft.Core.Abstractions;

namespace TrustDraft.Infrastructure.Persistence;

/// <summary>Lets `dotnet ef migrations add ...` build the context without running the API.</summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var conn = Environment.GetEnvironmentVariable("TRUSTDRAFT_DB")
                   ?? "Host=localhost;Port=5432;Database=trustdraft;Username=trustdraft;Password=trustdraft";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(conn, o => o.UseVector())
            .Options;

        return new AppDbContext(options, new DesignTimeTenant());
    }

    private sealed class DesignTimeTenant : ITenantContext
    {
        public Guid TenantId => Guid.Empty;
    }
}
