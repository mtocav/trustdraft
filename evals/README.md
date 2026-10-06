# Evals

Most LLM portfolio projects are demos. This folder is what makes TrustDraft measurable.

## Fixture: Acme IT B.V.

A fictional Dutch IT service provider (~40 staff) that sells to NIS2-regulated customers.

- `fixtures/acme-it/` — its policy documents (information security policy, access control,
  backup & recovery, incident response, supplier management, encryption, BCP, HR security, ...).
  Aim for ~10 realistic documents. Deliberately leave some gaps, so the system has to say
  "insufficient information" for some questions.
- `questions.jsonl` — one test case per line:

```json
{"id": "q001", "question": "...", "expected_verdict": "Yes|No|Partial|null", "expected_answer_contains": ["..."], "expected_sources": ["file.md"], "answerable": true}
```

## Metrics (week 7)

| Metric | What it measures |
|---|---|
| Answer accuracy | Verdict matches and the answer contains the expected key facts (LLM-as-judge + string checks) |
| Citation accuracy | Cited documents ⊆ expected sources |
| Hallucination rate | Unanswerable questions that got an answer instead of "insufficient information" |
| Reuse rate | Share of questions answered from the approved-answer library |

Results go to `evals/results/` (git-ignored); CI posts a summary and fails if a metric drops
below the threshold.
