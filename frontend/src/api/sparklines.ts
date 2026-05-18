import { useQuery } from '@tanstack/react-query'
import { apiClient } from './client'

export type SparklinePoint = { date: string; close: number }
export type SparklinesResponse = Record<string, SparklinePoint[]>

export function useSparklines(symbols: string[], days = 30) {
  const key = [...symbols].sort().join(',')
  return useQuery({
    queryKey: ['stocks', 'sparklines', key, days],
    enabled: symbols.length > 0,
    queryFn: async () => {
      const res = await apiClient.get<SparklinesResponse>('/api/stocks/sparklines', {
        params: { symbols: symbols.join(','), days },
      })
      return res.data
    },
    staleTime: 60 * 60_000,
  })
}
