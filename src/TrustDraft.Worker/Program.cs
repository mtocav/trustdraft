using TrustDraft.Core.Abstractions;
using TrustDraft.Infrastructure;
using TrustDraft.Worker;
using TrustDraft.Worker.Jobs;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddTrustDraftInfrastructure(builder.Configuration);

// The worker serves all tenants: the tenant is set per job before any DB work happens.
builder.Services.AddScoped<JobTenantContext>();
builder.Services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<JobTenantContext>());

builder.Services.AddScoped<IJobHandler, ProcessDocumentHandler>();
builder.Services.AddScoped<IJobHandler, ImportQuestionnaireHandler>();
builder.Services.AddScoped<IJobHandler, DraftAnswersHandler>();

builder.Services.AddHostedService<JobRunner>();

builder.Build().Run();
