import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { apiClient } from './client'

export type CashTransactionDto = {
  id: number
  type: 'Deposit' | 'Withdraw'
  amount: number
  balanceAfter: number
  executedAt: string
  notes: string | null
}

export type CashAdjustmentResponse = {
  transaction: CashTransactionDto
  cashBalance: number
}

export function useCashHistory() {
  return useQuery({
    queryKey: ['portfolio', 'cash'],
    queryFn: async () => {
      const res = await apiClient.get<CashTransactionDto[]>('/api/portfolio/cash')
      return res.data
    },
  })
}

export function useAdjustCash() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: async (input: { type: 'Deposit' | 'Withdraw'; amount: number; notes?: string }) => {
      const res = await apiClient.post<CashAdjustmentResponse>('/api/portfolio/cash', input)
      return res.data
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['portfolio'] })
      qc.invalidateQueries({ queryKey: ['user-me'] })
    },
  })
}
