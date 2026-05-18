import { useQuery } from '@tanstack/react-query'
import { apiClient } from './client'

export type CompanyProfileResponse = {
  symbol: string
  name: string
  logoUrl: string | null
  sector: string | null
  industry: string | null
  employees: number | null
  website: string | null
  description: string | null
  ceo: string | null
  marketCap: number | null
  peRatio: number | null
  eps: number | null
  dividendYield: number | null
  beta: number | null
  fiftyTwoWeekHigh: number | null
  fiftyTwoWeekLow: number | null
}

export function useStockProfile(symbol: string, enabled = true) {
  return useQuery({
    queryKey: ['stocks', 'profile', symbol.toUpperCase()],
    enabled: enabled && symbol.length > 0,
    queryFn: async () => {
      const res = await apiClient.get<CompanyProfileResponse>(
        `/api/stocks/${encodeURIComponent(symbol)}/profile`,
      )
      return res.data
    },
    staleTime: 24 * 60 * 60_000, // 24h on the frontend; backend has 7-day TTL
  })
}
