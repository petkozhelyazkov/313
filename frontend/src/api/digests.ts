import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { apiClient } from './client'

export type DigestSummary = {
  id: number
  subject: string
  periodStart: string
  periodEnd: string
  generatedAt: string
  sentAt: string | null
  read: boolean
}

export type DigestDetail = DigestSummary & {
  readAt: string | null
  bodyHtml: string
  bodyText: string
}

const digestKeys = {
  all: ['digests'] as const,
  detail: (id: number) => ['digests', id] as const,
}

export function useDigests() {
  return useQuery({
    queryKey: digestKeys.all,
    queryFn: async () => {
      const res = await apiClient.get<DigestSummary[]>('/api/digests')
      return res.data
    },
  })
}

export function useDigest(id: number | null) {
  return useQuery({
    queryKey: id === null ? digestKeys.all : digestKeys.detail(id),
    enabled: id !== null,
    queryFn: async () => {
      const res = await apiClient.get<DigestDetail>(`/api/digests/${id}`)
      return res.data
    },
  })
}

export function useGenerateDigest() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: async () => {
      const res = await apiClient.post<DigestSummary>('/api/digests/run-now')
      return res.data
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: digestKeys.all })
    },
  })
}

export function useUpdatePreferences() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: async (input: { emailDigestEnabled: boolean }) => {
      await apiClient.put('/api/users/me/preferences', input)
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['users', 'me'] })
    },
  })
}
