import {
  createContext,
  useCallback,
  useEffect,
  useMemo,
  useRef,
  useState,
  type ReactNode,
} from 'react'
import { useNavigate } from 'react-router-dom'
import {
  clearAccessToken,
  clearStoredUser,
  getAccessToken,
  getStoredUser,
  LOGGED_OUT_EVENT,
  setAccessToken,
  setStoredUser,
  type LoggedOutEventDetail,
} from './tokenStorage'
import { getCurrentUser } from '../api/users'
import type { AuthContextValue, AuthUser } from './types'

export const AuthContext = createContext<AuthContextValue | undefined>(undefined)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(() => getStoredUser())
  const [isLoading, setIsLoading] = useState<boolean>(() => Boolean(getAccessToken()))
  const navigate = useNavigate()
  const mountedRef = useRef(true)

  useEffect(() => {
    mountedRef.current = true
    return () => {
      mountedRef.current = false
    }
  }, [])

  const performLogout = useCallback(
    (returnTo?: string) => {
      clearAccessToken()
      clearStoredUser()
      setUser(null)
      if (returnTo && returnTo !== '/login' && !returnTo.startsWith('/login?')) {
        navigate(`/login?returnTo=${encodeURIComponent(returnTo)}`, { replace: true })
      } else {
        navigate('/login', { replace: true })
      }
    },
    [navigate],
  )

  // On mount: if there's a token, validate by calling /me.
  useEffect(() => {
    const token = getAccessToken()
    if (!token) {
      setIsLoading(false)
      return
    }
    let cancelled = false
    getCurrentUser()
      .then((u) => {
        if (cancelled) return
        setUser(u)
        setStoredUser(u)
      })
      .catch(() => {
        // 401 / 403 / network: drop the token; AuthContext will reflect logged-out state.
        // The Axios interceptor will already have cleared the token on 401 + fired the event.
        if (cancelled) return
        clearAccessToken()
        clearStoredUser()
        setUser(null)
      })
      .finally(() => {
        if (!cancelled) setIsLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [])

  // Listen for forced-logout from the Axios 401 handler.
  useEffect(() => {
    const handler = (event: Event) => {
      const detail = (event as CustomEvent<LoggedOutEventDetail>).detail
      performLogout(detail?.returnTo)
    }
    window.addEventListener(LOGGED_OUT_EVENT, handler)
    return () => window.removeEventListener(LOGGED_OUT_EVENT, handler)
  }, [performLogout])

  const login = useCallback((token: string, u: AuthUser, _expiresAt: string) => {
    setAccessToken(token)
    setStoredUser(u)
    setUser(u)
  }, [])

  const logout = useCallback(() => {
    performLogout()
  }, [performLogout])

  const refreshUser = useCallback(async () => {
    try {
      const u = await getCurrentUser()
      if (!mountedRef.current) return
      setUser(u)
      setStoredUser(u)
    } catch {
      /* interceptor already handled the 401 */
    }
  }, [])

  const value = useMemo<AuthContextValue>(
    () => ({
      user,
      isAuthenticated: Boolean(user),
      isLoading,
      login,
      logout,
      refreshUser,
      hasRole: (role: string) => user?.roles.includes(role) ?? false,
    }),
    [user, isLoading, login, logout, refreshUser],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
