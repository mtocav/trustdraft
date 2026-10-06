# Roadmap: MVP in 8 weeks (part-time)

Each `## Week` heading becomes a GitHub milestone and each `- [ ]` line becomes an issue
when you run `scripts/create-github-issues.ps1`. Text after ` — ` becomes the issue body.

## Week 0 — Customer discovery (runs alongside the build)
- [ ] Interview 5–10 IT suppliers / MSPs — Ask how many security questionnaires they get per month, how long each takes, who fills them in, and what tools they use now.
- [ ] Collect 3+ real (anonymised) questionnaires — Best test data for import and evals. Note the formats (xlsx layouts, PDF, portals).
- [ ] Write a one-page problem/solution summary — Keep it updated as interviews come in; becomes the landing page copy later.

## Week 1 — Foundation
- [ ] Run the scaffold locally — docker compose up, run API + worker + web, confirm /health is ok.
- [ ] Add the first EF Core migration — `dotnet ef migrations add Initial` from src/TrustDraft.Infrastructure with the API as startup project.
- [ ] Real authentication — Magic link or OIDC; tenant comes from a claim, delete the X-Tenant-Id dev header.
- [ ] Postgres row-level security — Second isolation layer behind the EF query filters; add a test that proves tenant A can't read tenant B.
- [ ] Audit log — Write AuditEvent rows for uploads, deletes, approvals and exports.

## Week 2 — Ingestion
- [ ] Document parsers — IDocumentParser for PDF (PdfPig), DOCX (OpenXML), Markdown/TXT, XLSX (ClosedXML).
- [ ] Chunking with locators — ~500–800 tokens with overlap; keep page/section/cell locators for citations.
- [ ] Embedding client — IEmbeddingClient behind an interface; EU-hosted or zero-retention provider. Batch requests.
- [ ] Implement ProcessDocumentHandler — Parse, chunk, embed, save Chunks; mark the Document Ready/Failed.

## Week 3 — Retrieval
- [ ] Full-text search column — Generated tsvector column + GIN index on Chunks (Dutch + English configs).
- [ ] Hybrid retriever — IRetriever combining pgvector cosine search and full-text search (reciprocal rank fusion).
- [ ] Retrieval debug endpoint — Dev-only endpoint that shows what's retrieved for a query; you'll use it constantly.

## Week 4 — Questionnaire import
- [ ] XLSX import — ImportQuestionnaireHandler: find header row, detect question/answer columns (NL + EN keywords).
- [ ] Column mapping UI — Let the user confirm or fix the detected columns before drafting.
- [ ] Handle messy sheets — Merged cells, multiple sheets, section headers between questions, Yes/No dropdown columns.

## Week 5 — Answer pipeline
- [ ] Real IAnswerGenerator — LLM call with structured JSON output: answer, verdict, citations, confidence, insufficient_info.
- [ ] Prompt rules — Answer only from the provided context; say insufficient info otherwise; match the question's language.
- [ ] Validation — Drop citations that weren't retrieved; low confidence goes to NeedsReview.

## Week 6 — Review & library
- [ ] Review screen — Question, draft and cited sources side by side; edit, approve, bulk-approve high-confidence answers.
- [ ] Answer library reuse — Embed approved questions; reuse near-duplicate approved answers before calling the LLM.
- [ ] Progress view — Per questionnaire: total / drafted / needs review / approved.

## Week 7 — Export & evals
- [ ] XLSX export — Write answers back into the original file at AnswerCellRef, keep formatting intact.
- [ ] Acme IT eval fixture — ~10 policy docs for a fictional company plus ~100 questions with expected answers (see evals/).
- [ ] Eval runner + CI job — Measure answer accuracy, citation accuracy and hallucination rate; fail CI on regressions.

## Week 8 — Ship it
- [ ] Deploy to an EU region — API, worker, Postgres, object storage (S3-compatible) in the EU; secrets out of appsettings.
- [ ] Answer your own security questionnaire with TrustDraft — Doubles as the best sales demo.
- [ ] Landing page + demo video — Problem, 60-second demo on the Acme IT fixture, contact/waitlist form.
- [ ] Portfolio write-up — README screenshots, architecture diagram, eval results; link it from your CV.
