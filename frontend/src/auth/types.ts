export type AuthUser = {
  id: string
  email: string
  displayName: string
  cashBalance: number
  isActive: boolean
  createdAt: string
  roles: string[]
  emailDigestEnabled?: boolean
}

export type AuthContextValue = {
  user: AuthUser | null
  isAuthenticated: boolean
  isLoading: boolean
  login: (token: string, user: AuthUser, expiresAt: string) => void
  logout: () => void
  refreshUser: () => Promise<void>
  hasRole: (role: string) => boolean
}

export type LoginApiResponse = {
  accessToken: string
  expiresAt: string
  user: AuthUser
}

export type RegisterApiResponse = {
  userId: string
  email: string
  displayName: string
}
