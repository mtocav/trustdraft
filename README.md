# TrustDraft

**Security questionnaires, answered from your own documents.**

Since the Dutch Cyberbeveiligingswet (NIS2) took effect on 15 August 2026, 8,000+ organisations have to vet
their critical suppliers. That means smaller IT suppliers get flooded with security questionnaires they have no
team to answer. TrustDraft lets a supplier upload its policies once, then pre-fills every new questionnaire with
answers grounded in those documents (with citations), routes uncertain answers to a human, and exports the
completed file.

> Status: scaffold / week 1 of the [roadmap](ROADMAP.md). Not legal advice and not a certification tool:
> it helps you prepare and answer.

## Architecture

```
React + TS (Vite)            web/
      │  REST (/api)
ASP.NET Core API             src/TrustDraft.Api
      │                          │
      │  jobs table              ▼
Background worker  ───────► Postgres + pgvector (relational + vectors + full-text)
src/TrustDraft.Worker
  ├─ parse → chunk → embed       (week 2)
  ├─ hybrid retrieval            (week 3)
  └─ answer pipeline → LLM       (week 5)

Files: IFileStorage (local disk in dev → S3-compatible EU storage in prod)
```

| Project | Purpose |
|---|---|
| `TrustDraft.Core` | Domain entities and interfaces (`IAnswerGenerator`, `IRetriever`, `IDocumentParser`, ...) |
| `TrustDraft.Infrastructure` | EF Core + Npgsql + pgvector, storage, AI provider implementations |
| `TrustDraft.Api` | Minimal API: documents, questionnaires, answers, approvals |
| `TrustDraft.Worker` | Postgres-backed job queue (`FOR UPDATE SKIP LOCKED`) and job handlers |
| `TrustDraft.Tests` | xUnit tests |
| `web/` | React + TypeScript frontend |
| `evals/` | Acme IT B.V. fixture + eval questions (accuracy, citations, hallucination rate) |

### Answer pipeline

1. **Library match**: reuse a near-duplicate *approved* answer if one exists.
2. **Hybrid retrieval**: pgvector + Postgres full-text search, rank fusion, top-k chunks.
3. **Structured generation**: `{ answer, verdict, citations[], confidence, insufficient_info }`.
4. **Validation**: drop citations that weren't retrieved; low confidence → *Needs review*.
5. **Human approval**, which feeds the library, so every questionnaire makes the next one faster.

## Getting started

Requirements: .NET 10 SDK, Node 22+, Docker Desktop.

```bash
# 1. Database (Postgres 17 + pgvector)
docker compose up -d

# 2. API  → http://localhost:5080  (creates the schema + a demo tenant on first run)
dotnet run --project src/TrustDraft.Api

# 3. Worker (second terminal)
dotnet run --project src/TrustDraft.Worker

# 4. Frontend (third terminal) → http://localhost:5173
cd web && npm install && npm run dev
```

Tests: `dotnet test` and `cd web && npm run build`.

### Migrations

In dev the API creates the schema with `EnsureCreated` until the first migration exists. Switch to migrations in week 1:

```bash
dotnet tool install --global dotnet-ef
dotnet ef migrations add Initial --project src/TrustDraft.Infrastructure --startup-project src/TrustDraft.Api
```

From then on the API applies migrations on startup in Development. Drop the dev database first
(`docker compose down -v`) so the schema `EnsureCreated` made doesn't clash with the migration.

### Turning the roadmap into GitHub issues

```powershell
gh auth login
./scripts/create-github-issues.ps1 -DryRun   # preview
./scripts/create-github-issues.ps1           # creates milestones per week + one issue per task
```

## Security principles (the tool must pass its own questionnaire)

- Tenant isolation: EF Core global query filters now, Postgres row-level security next (week 1).
- EU hosting for database, files and (where possible) the LLM; zero-retention LLM agreements; customer data never used for training.
- Audit log for uploads, deletes, approvals and exports. Hard delete on request.
- No secrets in `appsettings.json` outside local dev.

## License

All rights reserved (for now).
