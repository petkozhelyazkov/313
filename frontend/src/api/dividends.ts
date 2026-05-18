import { useQuery } from '@tanstack/react-query'
import { apiClient } from './client'

export type DividendHistoryItem = {
  symbol: string
  exDate: string
  paymentDate: string | null
  amountPerShare: number
}

export type UpcomingDividend = {
  symbol: string
  name: string | null
  logoUrl: string | null
  exDate: string
  paymentDate: string | null
  amountPerShare: number
  currentQuantity: number
  estimatedPayment: number
}

export type ReceivedDividend = {
  symbol: string
  exDate: string
  paymentDate: string | null
  amountPerShare: number
  quantityHeld: number
  totalReceived: number
}

export type DividendSummary = {
  lifetimeReceived: number
  upcoming30Days: number
  last12Months: number
  uniqueSymbols: number
}

export function useDividendHistory(symbol: string, enabled = true) {
  return useQuery({
    queryKey: ['dividends', 'history', symbol] as const,
    queryFn: async () => {
      const res = await apiClient.get<DividendHistoryItem[]>(`/api/dividends/history/${encodeURIComponent(symbol)}`)
      return res.data
    },
    enabled: enabled && symbol.length > 0,
    staleTime: 60 * 60_000,
  })
}

export function useUpcomingDividends() {
  return useQuery({
    queryKey: ['dividends', 'upcoming'] as const,
    queryFn: async () => {
      const res = await apiClient.get<UpcomingDividend[]>('/api/dividends/upcoming')
      return res.data
    },
    staleTime: 5 * 60_000,
  })
}

export function useReceivedDividends() {
  return useQuery({
    queryKey: ['dividends', 'received'] as const,
    queryFn: async () => {
      const res = await apiClient.get<ReceivedDividend[]>('/api/dividends/received')
      return res.data
    },
    staleTime: 5 * 60_000,
  })
}

export function useDividendSummary() {
  return useQuery({
    queryKey: ['dividends', 'summary'] as const,
    queryFn: async () => {
      const res = await apiClient.get<DividendSummary>('/api/dividends/summary')
      return res.data
    },
    staleTime: 5 * 60_000,
  })
}
