import { useQuery } from '@tanstack/react-query'
import { apiClient } from './client'

export type AnalyticsRange = '1M' | '3M' | '6M' | '1Y' | 'MAX'

export type SnapshotPoint = {
  date: string
  totalValue: number
  cashBalance: number
  holdingsValue: number
  totalInvested: number
  unrealizedPl: number
  benchmark: number | null
}

export type AllocationSlice = {
  symbol: string
  value: number
  weight: number
  quantity: number
}

export type ReturnsRow = {
  symbol: string
  unrealizedPl: number
  realizedPl: number
  totalPl: number
  totalPlPct: number | null
}

export type SectorSlice = {
  sector: string
  value: number
  weight: number
  symbols: number
}

export type RiskMetrics = {
  beta: number | null
  annualizedVolatility: number | null
  sharpeRatio: number | null
  maxDrawdown: number | null
  dataPoints: number
}

export type DiversificationResponse = {
  score: number
  positionsCount: number
  sectorsCount: number
  largestPositionPct: number
  largestSectorPct: number
  verdict: string
  suggestions: string[]
}

export const analyticsKeys = {
  all: ['analytics'] as const,
  snapshots: (range: AnalyticsRange) => [...analyticsKeys.all, 'snapshots', range] as const,
  allocation: () => [...analyticsKeys.all, 'allocation'] as const,
  returns: () => [...analyticsKeys.all, 'returns'] as const,
}

export function useAnalyticsSnapshots(range: AnalyticsRange, includeBenchmark = false) {
  return useQuery({
    queryKey: [...analyticsKeys.snapshots(range), includeBenchmark],
    queryFn: async () => {
      const res = await apiClient.get<SnapshotPoint[]>('/api/analytics/snapshots', {
        params: { range, includeBenchmark },
      })
      return res.data
    },
    refetchInterval: 5 * 60_000,
  })
}

export function useAnalyticsAllocation() {
  return useQuery({
    queryKey: analyticsKeys.allocation(),
    queryFn: async () => {
      const res = await apiClient.get<AllocationSlice[]>('/api/analytics/allocation')
      return res.data
    },
    refetchInterval: 60_000,
  })
}

export function useAnalyticsReturns() {
  return useQuery({
    queryKey: analyticsKeys.returns(),
    queryFn: async () => {
      const res = await apiClient.get<ReturnsRow[]>('/api/analytics/returns')
      return res.data
    },
    refetchInterval: 60_000,
  })
}

export function useAnalyticsSectorAllocation() {
  return useQuery({
    queryKey: [...analyticsKeys.all, 'sector-allocation'] as const,
    queryFn: async () => {
      const res = await apiClient.get<SectorSlice[]>('/api/analytics/sector-allocation')
      return res.data
    },
    refetchInterval: 60_000,
  })
}

export function useAnalyticsRisk() {
  return useQuery({
    queryKey: [...analyticsKeys.all, 'risk'] as const,
    queryFn: async () => {
      const res = await apiClient.get<RiskMetrics>('/api/analytics/risk')
      return res.data
    },
    refetchInterval: 5 * 60_000,
  })
}

export function useAnalyticsDiversification() {
  return useQuery({
    queryKey: [...analyticsKeys.all, 'diversification'] as const,
    queryFn: async () => {
      const res = await apiClient.get<DiversificationResponse>('/api/analytics/diversification')
      return res.data
    },
    refetchInterval: 5 * 60_000,
  })
}

export type AdvancedMetrics = {
  timeWeightedReturn: number | null
  moneyWeightedReturn: number | null
  sortinoRatio: number | null
  bestDayReturn: number | null
  bestDayDate: string | null
  worstDayReturn: number | null
  worstDayDate: string | null
  positiveDays: number
  negativeDays: number
  winRate: number | null
  averageDailyReturn: number | null
  dataPoints: number
  range: string
}

export function useAnalyticsAdvanced(range: AnalyticsRange) {
  return useQuery({
    queryKey: [...analyticsKeys.all, 'advanced', range] as const,
    queryFn: async () => {
      const res = await apiClient.get<AdvancedMetrics>('/api/analytics/advanced', { params: { range } })
      return res.data
    },
    refetchInterval: 5 * 60_000,
  })
}
