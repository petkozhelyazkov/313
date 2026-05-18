import { useQuery } from '@tanstack/react-query'
import { apiClient } from './client'
import type { AuthUser } from '../auth/types'

export type Achievement = {
  code: string
  name: string
  description: string
  icon: string
  earned: boolean
  earnedAt: string | null
  progress: number | null
  target: number | null
}

export function useMyAchievements() {
  return useQuery({
    queryKey: ['achievements', 'me'] as const,
    queryFn: async () => {
      const res = await apiClient.get<Achievement[]>('/api/users/me/achievements')
      return res.data
    },
    refetchInterval: 60_000,
  })
}

export async function getCurrentUser(): Promise<AuthUser> {
  const res = await apiClient.get<AuthUser>('/api/users/me')
  return res.data
}

export async function updateProfile(displayName: string): Promise<void> {
  await apiClient.put('/api/users/me', { displayName })
}

export async function changePassword(input: {
  currentPassword: string
  newPassword: string
}): Promise<void> {
  await apiClient.put('/api/users/me/password', input)
}
