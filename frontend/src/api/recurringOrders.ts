import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { apiClient } from './client'

export type RecurringFrequency = 'Daily' | 'Weekly' | 'Biweekly' | 'Monthly'

export type RecurringOrderDto = {
  id: number
  symbol: string
  cashAmount: number
  frequency: RecurringFrequency
  nextRunAt: string
  lastRunAt: string | null
  isActive: boolean
  successfulRuns: number
  failedRuns: number
  lastFailureReason: string | null
}

const KEY = ['recurring-orders'] as const

export function useRecurringOrders() {
  return useQuery({
    queryKey: KEY,
    queryFn: async () => {
      const res = await apiClient.get<RecurringOrderDto[]>('/api/recurring-orders')
      return res.data
    },
  })
}

export function useCreateRecurringOrder() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: async (input: {
      symbol: string
      cashAmount: number
      frequency: RecurringFrequency
      startAt?: string
    }) => {
      const res = await apiClient.post<RecurringOrderDto>('/api/recurring-orders', input)
      return res.data
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: KEY }),
  })
}

export function useUpdateRecurringOrder() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: async (input: {
      id: number
      cashAmount?: number
      frequency?: RecurringFrequency
      isActive?: boolean
    }) => {
      const { id, ...body } = input
      await apiClient.put(`/api/recurring-orders/${id}`, body)
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: KEY }),
  })
}

export function useDeleteRecurringOrder() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: async (id: number) => {
      await apiClient.delete(`/api/recurring-orders/${id}`)
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: KEY }),
  })
}
