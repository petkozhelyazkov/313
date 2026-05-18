import { useQuery } from '@tanstack/react-query'
import { apiClient } from './client'

export type EarningsCalendarItem = {
  symbol: string
  companyName: string | null
  logoUrl: string | null
  reportDate: string
  time: string | null
  epsEstimate: number | null
  epsActual: number | null
  isHeld: boolean
  isWatched: boolean
}

export function useEarningsCalendar(days = 7) {
  return useQuery({
    queryKey: ['analytics', 'earnings-calendar', days],
    queryFn: async () => {
      const res = await apiClient.get<EarningsCalendarItem[]>('/api/analytics/earnings-calendar', {
        params: { days },
      })
      return res.data
    },
    refetchInterval: 60 * 60_000, // hourly
  })
}
