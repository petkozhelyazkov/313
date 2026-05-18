import { useQuery } from '@tanstack/react-query'
import { apiClient } from './client'
import type { PositionDto } from './portfolio'

// ─── Types ───────────────────────────────────────────────────────────────────

export type StockSearchResult = {
  symbol: string
  name: string
  exchange: string | null
  currency: string
  country: string | null
  instrumentType: string | null
  logoUrl: string | null
}

export type QuoteResponse = {
  symbol: string
  price: number
  dayChange: number | null
  dayChangePct: number | null
  previousClose: number | null
  volume: number
  fetchedAt: string
  isStale: boolean
}

export type HistoryPoint = {
  date: string
  open: number
  high: number
  low: number
  close: number
  volume: number
}

export type HistoryResponse = {
  symbol: string
  range: string
  points: HistoryPoint[]
}

export type StockDetailResponse = {
  stock: StockSearchResult
  quote: QuoteResponse | null
  history: HistoryResponse | null
  userPosition: PositionDto | null
  inWatchlist: boolean
}

export type AnalystConsensus = {
  symbol: string
  numAnalysts: number
  recommendationMean: number | null
  verdictLabel: string
  strongBuy: number
  buy: number
  hold: number
  sell: number
  strongSell: number
  targetLow: number | null
  targetMean: number | null
  targetHigh: number | null
  currentPrice: number | null
  upsidePct: number | null
  fetchedAt: string
}

export type InsiderTrade = {
  id: number
  symbol: string
  personName: string
  role: string | null
  transactionDate: string
  transactionType: string
  shares: number
  pricePerShare: number | null
  value: number | null
}

export type InsiderSummary = {
  symbol: string
  last90DaysBuyCount: number
  last90DaysSellCount: number
  last90DaysBuyValue: number
  last90DaysSellValue: number
  recentTrades: InsiderTrade[]
}

export function useAnalyst(symbol: string, enabled = true) {
  return useQuery({
    queryKey: [...stockKeys.all, 'analyst', symbol.toUpperCase()] as const,
    enabled: enabled && symbol.trim().length > 0,
    queryFn: async () => {
      try {
        const res = await apiClient.get<AnalystConsensus>(`/api/stocks/${encodeURIComponent(symbol)}/analyst`)
        return res.data
      } catch (err: unknown) {
        const e = err as { response?: { status?: number } }
        if (e.response?.status === 404) return null
        throw err
      }
    },
    staleTime: 24 * 60 * 60_000,
  })
}

export function useInsiders(symbol: string, enabled = true) {
  return useQuery({
    queryKey: [...stockKeys.all, 'insiders', symbol.toUpperCase()] as const,
    enabled: enabled && symbol.trim().length > 0,
    queryFn: async () => {
      const res = await apiClient.get<InsiderSummary>(`/api/stocks/${encodeURIComponent(symbol)}/insiders`)
      return res.data
    },
    staleTime: 6 * 60 * 60_000,
  })
}

export type Range = '1M' | '3M' | '6M' | '1Y' | '5Y' | 'MAX'

// ─── Query keys ──────────────────────────────────────────────────────────────

export const stockKeys = {
  all: ['stocks'] as const,
  search: (q: string) => [...stockKeys.all, 'search', q] as const,
  quote: (symbol: string) => [...stockKeys.all, 'quote', symbol.toUpperCase()] as const,
  history: (symbol: string, range: Range) =>
    [...stockKeys.all, 'history', symbol.toUpperCase(), range] as const,
  detail: (symbol: string) => [...stockKeys.all, 'detail', symbol.toUpperCase()] as const,
}

// ─── Hooks ───────────────────────────────────────────────────────────────────

export function useSymbolSearch(query: string, enabled = true) {
  return useQuery({
    queryKey: stockKeys.search(query),
    enabled: enabled && query.trim().length > 0,
    queryFn: async () => {
      const res = await apiClient.get<StockSearchResult[]>('/api/stocks/search', {
        params: { q: query, limit: 10 },
      })
      return res.data
    },
    staleTime: 5 * 60_000,
  })
}

export function useQuote(symbol: string, enabled = true) {
  return useQuery({
    queryKey: stockKeys.quote(symbol),
    enabled: enabled && symbol.length > 0,
    queryFn: async () => {
      const res = await apiClient.get<QuoteResponse>(`/api/stocks/${encodeURIComponent(symbol)}/quote`)
      return res.data
    },
    refetchInterval: 60_000,
  })
}

export function useHistory(symbol: string, range: Range, enabled = true) {
  return useQuery({
    queryKey: stockKeys.history(symbol, range),
    enabled: enabled && symbol.length > 0,
    queryFn: async () => {
      const res = await apiClient.get<HistoryResponse>(`/api/stocks/${encodeURIComponent(symbol)}/history`, {
        params: { range },
      })
      return res.data
    },
    staleTime: 60 * 60_000,
  })
}

export function useStockDetail(symbol: string, enabled = true) {
  return useQuery({
    queryKey: stockKeys.detail(symbol),
    enabled: enabled && symbol.length > 0,
    queryFn: async () => {
      const res = await apiClient.get<StockDetailResponse>(`/api/stocks/${encodeURIComponent(symbol)}`)
      return res.data
    },
    refetchInterval: 60_000,
  })
}
