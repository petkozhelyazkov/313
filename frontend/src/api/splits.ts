import { useQuery } from '@tanstack/react-query'
import { apiClient } from './client'

export type SplitHistoryItem = {
  symbol: string
  date: string
  fromFactor: number
  toFactor: number
  ratio: string
}

export function useSplitHistory(symbol: string, enabled = true) {
  return useQuery({
    queryKey: ['splits', 'history', symbol] as const,
    queryFn: async () => {
      const res = await apiClient.get<SplitHistoryItem[]>(`/api/splits/history/${encodeURIComponent(symbol)}`)
      return res.data
    },
    enabled: enabled && symbol.length > 0,
    staleTime: 24 * 60 * 60_000,
  })
}
