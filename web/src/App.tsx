import { useCallback, useEffect, useState } from 'react'
import { api, type DocumentSummary, type QuestionnaireSummary } from './api'

function useLoad<T>(load: () => Promise<T>) {
  const [data, setData] = useState<T | null>(null)
  const [error, setError] = useState<string | null>(null)
  const reload = useCallback(() => {
    load().then(setData, (e: Error) => setError(e.message))
  }, [load])
  useEffect(reload, [reload])
  return { data, error, reload }
}

function UploadButton({ label, accept, onUpload }: { label: string; accept: string; onUpload: (f: File) => Promise<unknown> }) {
  const [busy, setBusy] = useState(false)
  return (
    <label className={`button ${busy ? 'busy' : ''}`}>
      {busy ? 'Uploading…' : label}
      <input
        type="file"
        accept={accept}
        hidden
        disabled={busy}
        onChange={async (e) => {
          const file = e.target.files?.[0]
          e.target.value = ''
          if (!file) return
          setBusy(true)
          try {
            await onUpload(file)
          } catch (err) {
            alert((err as Error).message)
          } finally {
            setBusy(false)
          }
        }}
      />
    </label>
  )
}

function StatusPill({ status }: { status: string }) {
  return <span className={`pill pill-${status.toLowerCase()}`}>{status}</span>
}

export default function App() {
  const health = useLoad(api.health)
  const docs = useLoad<DocumentSummary[]>(api.documents.list)
  const questionnaires = useLoad<QuestionnaireSummary[]>(api.questionnaires.list)

  return (
    <main>
      <header>
        <h1>TrustDraft</h1>
        <p className="muted">Security questionnaires, answered from your own documents.</p>
        <p className="muted small">
          API: {health.data ? 'connected' : health.error ? `unreachable (${health.error})` : 'checking…'}
        </p>
      </header>

      <section>
        <div className="section-head">
          <h2>Knowledge base</h2>
          <UploadButton
            label="Upload document"
            accept=".pdf,.docx,.md,.txt,.xlsx"
            onUpload={async (f) => { await api.documents.upload(f); docs.reload() }}
          />
        </div>
        {docs.error && <p className="error">{docs.error}</p>}
        {docs.data?.length === 0 && <p className="muted">No documents yet. Upload your security policies to get started.</p>}
        <ul className="list">
          {docs.data?.map((d) => (
            <li key={d.id}>
              <span>{d.fileName}</span>
              <span className="muted small">{d.kind} · {d.chunkCount} chunks</span>
              <StatusPill status={d.status} />
            </li>
          ))}
        </ul>
      </section>

      <section>
        <div className="section-head">
          <h2>Questionnaires</h2>
          <UploadButton
            label="Upload questionnaire (.xlsx)"
            accept=".xlsx"
            onUpload={async (f) => { await api.questionnaires.upload(f); questionnaires.reload() }}
          />
        </div>
        {questionnaires.error && <p className="error">{questionnaires.error}</p>}
        {questionnaires.data?.length === 0 && <p className="muted">No questionnaires yet.</p>}
        <ul className="list">
          {questionnaires.data?.map((q) => (
            <li key={q.id}>
              <span>{q.name}</span>
              <span className="muted small">{q.approved}/{q.total} approved</span>
              <StatusPill status={q.status} />
              <button onClick={() => api.questionnaires.draft(q.id).then(questionnaires.reload)}>Draft answers</button>
            </li>
          ))}
        </ul>
      </section>
    </main>
  )
}
