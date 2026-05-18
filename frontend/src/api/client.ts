import axios, { AxiosError, type InternalAxiosRequestConfig } from 'axios'
import {
  clearAccessToken,
  clearStoredUser,
  emitLoggedOut,
  getAccessToken,
} from '../auth/tokenStorage'

const baseURL = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:7000'

export const apiClient = axios.create({
  baseURL,
  timeout: 15000,
  headers: { 'Content-Type': 'application/json' },
})

apiClient.interceptors.request.use((config: InternalAxiosRequestConfig) => {
  const token = getAccessToken()
  if (token) {
    config.headers.set('Authorization', `Bearer ${token}`)
  }
  return config
})

apiClient.interceptors.response.use(
  (response) => response,
  (error: AxiosError) => {
    const status = error.response?.status
    if (status === 401) {
      // The /api/auth/login endpoint also returns 401 on bad creds — don't kick
      // the user out for that. Only force logout for *authenticated* 401s.
      const url = error.config?.url ?? ''
      const isLoginAttempt = url.includes('/api/auth/login')
      if (!isLoginAttempt) {
        clearAccessToken()
        clearStoredUser()
        const returnTo = window.location.pathname + window.location.search
        emitLoggedOut(returnTo)
      }
    }
    return Promise.reject(error)
  },
)

export function isApiError(err: unknown): err is AxiosError {
  return axios.isAxiosError(err)
}

/** Extract a human-friendly error message from an Axios error (ProblemDetails-aware). */
export function getApiErrorMessage(err: unknown): string {
  if (!isApiError(err)) {
    return err instanceof Error ? err.message : 'Unknown error'
  }
  const data = err.response?.data as
    | { title?: string; detail?: string; errors?: Record<string, string[]> }
    | undefined
  if (data?.detail) return data.detail
  if (data?.errors) {
    const first = Object.values(data.errors).flat()[0]
    if (first) return first
  }
  if (data?.title) return data.title
  return err.message || 'Request failed'
}
