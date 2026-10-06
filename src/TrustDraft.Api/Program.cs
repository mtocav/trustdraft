using System.Text.Json.Serialization;
using TrustDraft.Api;
using TrustDraft.Api.Endpoints;
using TrustDraft.Core.Abstractions;
using TrustDraft.Infrastructure;
using TrustDraft.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantContext, HttpTenantContext>();
builder.Services.AddTrustDraftInfrastructure(builder.Configuration);

builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? new[] { "http://localhost:5173" })
    .AllowAnyHeader()
    .AllowAnyMethod()));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    await DevSeed.RunAsync(app.Services);
}

app.UseExceptionHandler();
app.UseCors();

app.MapGet("/health", async (AppDbContext db) =>
    await db.Database.CanConnectAsync() ? Results.Ok(new { status = "ok" }) : Results.Problem("Database unreachable"));

app.MapGroup("/api/documents").MapDocumentEndpoints();
app.MapGroup("/api/questionnaires").MapQuestionnaireEndpoints();

app.Run();
