using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pgvector.EntityFrameworkCore;
using TrustDraft.Core.Abstractions;
using TrustDraft.Infrastructure.Ai;
using TrustDraft.Infrastructure.Persistence;
using TrustDraft.Infrastructure.Storage;

namespace TrustDraft.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddTrustDraftInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        var conn = config.GetConnectionString("Default")
                   ?? throw new InvalidOperationException("ConnectionStrings:Default is not configured.");

        services.AddDbContext<AppDbContext>(o => o.UseNpgsql(conn, npgsql => npgsql.UseVector()));

        services.Configure<StorageOptions>(config.GetSection("Storage"));
        services.AddSingleton<IFileStorage, LocalFileStorage>();

        services.AddScoped<IAnswerGenerator, StubAnswerGenerator>();
        // TODO: IDocumentParser implementations (week 2), IEmbeddingClient + IRetriever (weeks 2–3).

        return services;
    }
}
