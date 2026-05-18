import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { apiClient } from './client'

export type GoalType = 'PortfolioValue' | 'TotalReturn' | 'DividendIncome'

export type GoalDto = {
  id: number
  type: GoalType
  targetAmount: number
  currentAmount: number
  progressPct: number
  title: string | null
  dueDate: string | null
  createdAt: string
  isCompleted: boolean
  completedAt: string | null
}

const KEY = ['goals'] as const

export function useGoals() {
  return useQuery({
    queryKey: KEY,
    queryFn: async () => {
      const res = await apiClient.get<GoalDto[]>('/api/goals')
      return res.data
    },
    refetchInterval: 60_000,
  })
}

export function useCreateGoal() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: async (input: {
      type: GoalType
      targetAmount: number
      title?: string | null
      dueDate?: string | null
    }) => {
      const res = await apiClient.post<GoalDto>('/api/goals', input)
      return res.data
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: KEY }),
  })
}

export function useUpdateGoal() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: async (input: {
      id: number
      targetAmount?: number
      title?: string | null
      dueDate?: string | null
      isCompleted?: boolean
    }) => {
      const { id, ...body } = input
      await apiClient.put(`/api/goals/${id}`, body)
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: KEY }),
  })
}

export function useDeleteGoal() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: async (id: number) => {
      await apiClient.delete(`/api/goals/${id}`)
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: KEY }),
  })
}
