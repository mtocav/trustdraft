export type ProcessingStatus = 'Pending' | 'Processing' | 'Ready' | 'Failed'

export interface DocumentSummary {
  id: string
  fileName: string
  kind: 'Policy' | 'PastQuestionnaire' | 'Other'
  status: ProcessingStatus
  createdAt: string
  chunkCount: number
}

export interface QuestionnaireSummary {
  id: string
  name: string
  requestedBy: string | null
  status: ProcessingStatus
  createdAt: string
  total: number
  approved: number
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const res = await fetch(path, init)
  if (!res.ok) {
    const body = await res.text()
    throw new Error(`${res.status} ${res.statusText}: ${body}`)
  }
  return (res.status === 204 ? undefined : await res.json()) as T
}

function upload<T>(path: string, file: File): Promise<T> {
  const form = new FormData()
  form.append('file', file)
  return request<T>(path, { method: 'POST', body: form })
}

export const api = {
  health: () => request<{ status: string }>('/health'),
  documents: {
    list: () => request<DocumentSummary[]>('/api/documents'),
    upload: (file: File) => upload<{ id: string }>('/api/documents', file),
    remove: (id: string) => request<void>(`/api/documents/${id}`, { method: 'DELETE' }),
  },
  questionnaires: {
    list: () => request<QuestionnaireSummary[]>('/api/questionnaires'),
    upload: (file: File) => upload<{ id: string }>('/api/questionnaires', file),
    draft: (id: string) => request<void>(`/api/questionnaires/${id}/draft`, { method: 'POST' }),
  },
}
