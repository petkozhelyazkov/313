import { apiClient } from './client'
import type { LoginApiResponse, RegisterApiResponse } from '../auth/types'

export async function login(email: string, password: string): Promise<LoginApiResponse> {
  const res = await apiClient.post<LoginApiResponse>('/api/auth/login', { email, password })
  return res.data
}

export async function register(input: {
  email: string
  password: string
  displayName: string
}): Promise<RegisterApiResponse> {
  const res = await apiClient.post<RegisterApiResponse>('/api/auth/register', input)
  return res.data
}
