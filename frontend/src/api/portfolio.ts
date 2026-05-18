import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { apiClient, getApiErrorMessage } from './client'

// ─── Shared types ────────────────────────────────────────────────────────────

export type PositionDto = {
  symbol: string
  quantity: number
  averageCost: number
  totalInvested: number
  realizedPlLifetime: number
  currentPrice: number | null
  currentValue: number | null
  unrealizedPl: number | null
  unrealizedPlPct: number | null
  weight: number | null
  firstPurchasedAt: string
  lastTransactionAt: string
  isClosed: boolean
  logoUrl: string | null
  name: string | null
  notes: string | null
  tags: string | null
}

export type PortfolioSummary = {
  cashBalance: number
  holdingsValue: number
  totalValue: number
  totalInvested: number
  unrealizedPl: number
  unrealizedPlPct: number
  realizedPlLifetime: number
  positions: PositionDto[]
}

export type TransactionDto = {
  id: number
  symbol: string
  type: 'Buy' | 'Sell'
  quantity: number
  pricePerShare: number
  fees: number
  totalAmount: number
  executedAt: string
  realizedPl: number | null
  notes: string | null
  tags: string | null
}

export type TagPlSummary = {
  tag: string
  realizedPl: number
  transactionCount: number
}

export type TransactionListResponse = {
  page: number
  pageSize: number
  totalCount: number
  items: TransactionDto[]
}

export type TradeResponse = {
  transaction: TransactionDto
  position: PositionDto
  cashBalance: number
}

// ─── Query keys ──────────────────────────────────────────────────────────────

export const portfolioKeys = {
  all: ['portfolio'] as const,
  summary: (includeClosed = false) => [...portfolioKeys.all, 'summary', includeClosed] as const,
  transactions: (page: number, pageSize: number, symbol?: string) =>
    [...portfolioKeys.all, 'transactions', page, pageSize, symbol ?? null] as const,
}

// ─── Fetch helpers ───────────────────────────────────────────────────────────

async function fetchPortfolioSummary(includeClosed = false): Promise<PortfolioSummary> {
  const res = await apiClient.get<PortfolioSummary>('/api/portfolio', {
    params: { includeClosed },
  })
  return res.data
}

async function fetchTransactions(
  page: number,
  pageSize: number,
  symbol?: string,
  tag?: string,
): Promise<TransactionListResponse> {
  const res = await apiClient.get<TransactionListResponse>('/api/portfolio/transactions', {
    params: { page, pageSize, symbol, tag },
  })
  return res.data
}

// ─── Hooks ───────────────────────────────────────────────────────────────────

export function usePortfolioSummary(includeClosed = false) {
  return useQuery({
    queryKey: portfolioKeys.summary(includeClosed),
    queryFn: () => fetchPortfolioSummary(includeClosed),
    refetchInterval: 60_000,
  })
}

export function useTransactions(page: number, pageSize: number, symbol?: string, tag?: string) {
  return useQuery({
    queryKey: [...portfolioKeys.transactions(page, pageSize, symbol), tag ?? null] as const,
    queryFn: () => fetchTransactions(page, pageSize, symbol, tag),
    placeholderData: (prev) => prev,
  })
}

export function useUpdateTransactionMutation() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: async (input: { id: number; notes?: string | null; tags?: string | null }) => {
      const { id, ...body } = input
      await apiClient.put(`/api/portfolio/transactions/${id}`, body)
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: portfolioKeys.all })
    },
  })
}

export function useTagPlSummary() {
  return useQuery({
    queryKey: [...portfolioKeys.all, 'tag-summary'] as const,
    queryFn: async () => {
      const res = await apiClient.get<TagPlSummary[]>('/api/portfolio/transactions/tag-summary')
      return res.data
    },
  })
}

export function useBuyMutation() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: async (input: { symbol: string; quantity: number; notes?: string }) => {
      const res = await apiClient.post<TradeResponse>('/api/portfolio/buy', input)
      return res.data
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: portfolioKeys.all })
    },
  })
}

export function useSellMutation() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: async (input: { symbol: string; quantity: number; notes?: string }) => {
      const res = await apiClient.post<TradeResponse>('/api/portfolio/sell', input)
      return res.data
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: portfolioKeys.all })
    },
  })
}

export function useUpdatePositionMutation() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: async (input: { symbol: string; notes?: string | null; tags?: string | null }) => {
      const { symbol, ...body } = input
      await apiClient.put(`/api/portfolio/positions/${encodeURIComponent(symbol)}`, body)
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: portfolioKeys.all })
    },
  })
}

export type TaxSellRow = {
  symbol: string
  acquiredAt: string
  soldAt: string
  quantity: number
  costBasis: number
  proceeds: number
  gain: number
  isLongTerm: boolean
}

export type TaxDividendRow = {
  symbol: string
  exDate: string
  amountPerShare: number
  quantityAtExDate: number
  totalReceived: number
}

export type TaxReport = {
  year: number
  shortTermGains: number
  shortTermLosses: number
  shortTermNet: number
  longTermGains: number
  longTermLosses: number
  longTermNet: number
  dividendsReceived: number
  feesPaid: number
  netTotal: number
  sellRows: TaxSellRow[]
  dividendRows: TaxDividendRow[]
}

export function useTaxYears() {
  return useQuery({
    queryKey: [...portfolioKeys.all, 'tax-years'] as const,
    queryFn: async () => {
      const res = await apiClient.get<number[]>('/api/portfolio/tax-report/years')
      return res.data
    },
  })
}

export function useTaxReport(year: number | null) {
  return useQuery({
    queryKey: [...portfolioKeys.all, 'tax-report', year] as const,
    enabled: year !== null,
    queryFn: async () => {
      const res = await apiClient.get<TaxReport>(`/api/portfolio/tax-report/${year}`)
      return res.data
    },
  })
}

export function tradeErrorMessage(err: unknown): string {
  return getApiErrorMessage(err)
}
