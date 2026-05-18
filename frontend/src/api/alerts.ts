import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { apiClient } from './client'

export type AlertDirection = 'Above' | 'Below'
export type AlertStatus = 'Active' | 'Triggered' | 'Cancelled'

export type PriceAlertDto = {
  id: number
  symbol: string
  name: string | null
  logoUrl: string | null
  direction: AlertDirection
  triggerPrice: number
  status: AlertStatus
  currentPrice: number | null
  acknowledged: boolean
  createdAt: string
  triggeredAt: string | null
  triggeredPrice: number | null
  notes: string | null
}

export const alertKeys = {
  all: ['alerts'] as const,
  list: () => [...alertKeys.all, 'list'] as const,
}

export function useAlerts() {
  return useQuery({
    queryKey: alertKeys.list(),
    queryFn: async () => {
      const res = await apiClient.get<PriceAlertDto[]>('/api/alerts')
      return res.data
    },
    refetchInterval: 60_000,
  })
}

export function useCreateAlert() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: async (input: { symbol: string; direction: AlertDirection; triggerPrice: number; notes?: string }) => {
      const res = await apiClient.post<PriceAlertDto>('/api/alerts', input)
      return res.data
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: alertKeys.all }),
  })
}

export function useCancelAlert() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: async (id: number) => {
      await apiClient.delete(`/api/alerts/${id}`)
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: alertKeys.all }),
  })
}

export function useAcknowledgeAlert() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: async (id: number) => {
      await apiClient.post(`/api/alerts/${id}/ack`)
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: alertKeys.all }),
  })
}
