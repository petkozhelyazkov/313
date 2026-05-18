import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { apiClient } from './client'
import type { QuoteResponse } from './stocks'
import { stockKeys } from './stocks'

export type WatchlistItemDto = {
  id: number
  symbol: string
  notes: string | null
  addedAt: string
  quote: QuoteResponse | null
  logoUrl: string | null
  name: string | null
  listName: string
}

export type WatchlistSummary = {
  listName: string
  count: number
}

export const watchlistKeys = {
  all: ['watchlist'] as const,
  list: (name?: string) => [...watchlistKeys.all, 'list', name ?? '__all'] as const,
  lists: () => [...watchlistKeys.all, 'lists'] as const,
}

export function useWatchlist(listName?: string) {
  return useQuery({
    queryKey: watchlistKeys.list(listName),
    queryFn: async () => {
      const params = listName ? { list: listName } : undefined
      const res = await apiClient.get<WatchlistItemDto[]>('/api/watchlist', { params })
      return res.data
    },
    refetchInterval: 60_000,
  })
}

export function useWatchlistLists() {
  return useQuery({
    queryKey: watchlistKeys.lists(),
    queryFn: async () => {
      const res = await apiClient.get<WatchlistSummary[]>('/api/watchlist/lists')
      return res.data
    },
  })
}

export function useAddToWatchlist() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: async (input: { symbol: string; notes?: string; listName?: string }) => {
      const res = await apiClient.post<WatchlistItemDto>('/api/watchlist', input)
      return res.data
    },
    onSuccess: (item) => {
      qc.invalidateQueries({ queryKey: watchlistKeys.all })
      qc.invalidateQueries({ queryKey: stockKeys.detail(item.symbol) })
    },
  })
}

export function useRemoveFromWatchlist() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: async (input: { symbol: string; listName?: string }) => {
      const params = input.listName ? { list: input.listName } : undefined
      await apiClient.delete(`/api/watchlist/${encodeURIComponent(input.symbol)}`, { params })
      return input.symbol
    },
    onSuccess: (symbol) => {
      qc.invalidateQueries({ queryKey: watchlistKeys.all })
      qc.invalidateQueries({ queryKey: stockKeys.detail(symbol) })
    },
  })
}

export function useRenameWatchlist() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: async (input: { oldName: string; newName: string }) => {
      await apiClient.put(`/api/watchlist/lists/${encodeURIComponent(input.oldName)}/rename`, {
        newName: input.newName,
      })
      return input
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: watchlistKeys.all }),
  })
}

export function useDeleteWatchlist() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: async (listName: string) => {
      await apiClient.delete(`/api/watchlist/lists/${encodeURIComponent(listName)}`)
      return listName
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: watchlistKeys.all }),
  })
}
