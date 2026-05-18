import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { apiClient } from './client'

export type AdminUserDto = {
  id: string
  email: string
  displayName: string
  cashBalance: number
  isActive: boolean
  createdAt: string
  roles: string[]
}

export type AdminUserListResponse = {
  page: number
  pageSize: number
  totalCount: number
  items: AdminUserDto[]
}

export type ApiUsageResponse = {
  today: { count: number; quota: number; percentUsed: number }
  lastHour: { count: number; quota: number; percentUsed: number }
  recentCalls: {
    id: number
    endpoint: string
    symbols: string | null
    requestedAt: string
    statusCode: number
    responseTimeMs: number
  }[]
}

export const adminKeys = {
  all: ['admin'] as const,
  users: (page: number, pageSize: number, email?: string) =>
    [...adminKeys.all, 'users', page, pageSize, email ?? null] as const,
  apiUsage: () => [...adminKeys.all, 'apiUsage'] as const,
}

export function useAdminUsers(page: number, pageSize: number, email?: string) {
  return useQuery({
    queryKey: adminKeys.users(page, pageSize, email),
    queryFn: async () => {
      const res = await apiClient.get<AdminUserListResponse>('/api/admin/users', {
        params: { page, pageSize, email },
      })
      return res.data
    },
    placeholderData: (prev) => prev,
  })
}

export function useSetRole() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: async (input: { id: string; role: 'User' | 'Admin' }) => {
      const res = await apiClient.put<AdminUserDto>(
        `/api/admin/users/${encodeURIComponent(input.id)}/role`,
        { role: input.role },
      )
      return res.data
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: adminKeys.all }),
  })
}

export function useSetActive() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: async (input: { id: string; isActive: boolean }) => {
      const res = await apiClient.put<AdminUserDto>(
        `/api/admin/users/${encodeURIComponent(input.id)}/active`,
        { isActive: input.isActive },
      )
      return res.data
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: adminKeys.all }),
  })
}

export function useApiUsage() {
  return useQuery({
    queryKey: adminKeys.apiUsage(),
    queryFn: async () => {
      const res = await apiClient.get<ApiUsageResponse>('/api/admin/api-usage')
      return res.data
    },
    refetchInterval: 30_000,
  })
}

export function useRunSnapshots() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: async () => {
      const res = await apiClient.post<{ date: string; processedUsers: number }>(
        '/api/admin/snapshots/run-now',
      )
      return res.data
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['analytics'] })
    },
  })
}
