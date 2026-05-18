const ACCESS_TOKEN_KEY = 'trading212.accessToken'
const USER_KEY = 'trading212.user'

export type StoredUser = {
  id: string
  email: string
  displayName: string
  cashBalance: number
  isActive: boolean
  createdAt: string
  roles: string[]
}

export function getAccessToken(): string | null {
  try {
    return localStorage.getItem(ACCESS_TOKEN_KEY)
  } catch {
    return null
  }
}

export function setAccessToken(token: string): void {
  try {
    localStorage.setItem(ACCESS_TOKEN_KEY, token)
  } catch {
    /* ignore quota / privacy mode */
  }
}

export function clearAccessToken(): void {
  try {
    localStorage.removeItem(ACCESS_TOKEN_KEY)
  } catch {
    /* ignore */
  }
}

export function getStoredUser(): StoredUser | null {
  try {
    const raw = localStorage.getItem(USER_KEY)
    return raw ? (JSON.parse(raw) as StoredUser) : null
  } catch {
    return null
  }
}

export function setStoredUser(user: StoredUser): void {
  try {
    localStorage.setItem(USER_KEY, JSON.stringify(user))
  } catch {
    /* ignore */
  }
}

export function clearStoredUser(): void {
  try {
    localStorage.removeItem(USER_KEY)
  } catch {
    /* ignore */
  }
}

export type LoggedOutEventDetail = { returnTo: string }
export const LOGGED_OUT_EVENT = 'trading212:logged-out'

/**
 * Fired by the Axios interceptor when a 401 lands. AuthContext listens and
 * clears its in-memory state + navigates the SPA to /login.
 */
export function emitLoggedOut(returnTo: string): void {
  window.dispatchEvent(
    new CustomEvent<LoggedOutEventDetail>(LOGGED_OUT_EVENT, { detail: { returnTo } }),
  )
}
