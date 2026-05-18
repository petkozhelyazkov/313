import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { apiClient } from './client'

export type OrderSide = 'LimitBuy' | 'LimitSell' | 'StopLoss' | 'TrailingStop'
export type OrderStatus = 'Pending' | 'Filled' | 'Cancelled' | 'Expired' | 'FailedExecution'

export type PendingOrderDto = {
  id: number
  symbol: string
  name: string | null
  logoUrl: string | null
  side: OrderSide
  status: OrderStatus
  quantity: number
  limitPrice: number
  filledPrice: number | null
  createdAt: string
  filledAt: string | null
  failureReason: string | null
  notes: string | null
  currentPrice: number | null
  trailingStopPercent: number | null
  highWaterMark: number | null
  currentTrigger: number | null
}

export type OrderListResponse = {
  open: PendingOrderDto[]
  history: PendingOrderDto[]
}

export const orderKeys = {
  all: ['orders'] as const,
  list: () => [...orderKeys.all, 'list'] as const,
}

export function useOrders() {
  return useQuery({
    queryKey: orderKeys.list(),
    queryFn: async () => {
      const res = await apiClient.get<OrderListResponse>('/api/orders')
      return res.data
    },
    refetchInterval: 60_000,
  })
}

export function usePlaceOrder() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: async (input: {
      symbol: string
      side: OrderSide
      quantity: number
      limitPrice: number
      trailingStopPercent?: number
      notes?: string
    }) => {
      const res = await apiClient.post<PendingOrderDto>('/api/orders', input)
      return res.data
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: orderKeys.all }),
  })
}

export function useCancelOrder() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: async (id: number) => {
      const res = await apiClient.delete<PendingOrderDto>(`/api/orders/${id}`)
      return res.data
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: orderKeys.all }),
  })
}
