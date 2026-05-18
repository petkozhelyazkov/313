import { useQuery } from '@tanstack/react-query'
import { apiClient } from './client'

export type MoverItem = {
  symbol: string
  name: string | null
  exchange: string | null
  logoUrl: string | null
  price: number
  change: number | null
  percentChange: number | null
}

export type MarketMoversResponse = {
  gainers: MoverItem[]
  losers: MoverItem[]
  actives: MoverItem[]
  fetchedAt: string
}

export function useMarketMovers() {
  return useQuery({
    queryKey: ['stocks', 'movers'],
    queryFn: async () => {
      const res = await apiClient.get<MarketMoversResponse>('/api/stocks/movers')
      return res.data
    },
    refetchInterval: 5 * 60_000,
  })
}
