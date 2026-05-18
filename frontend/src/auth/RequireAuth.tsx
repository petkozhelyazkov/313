import { Navigate, Outlet, useLocation } from 'react-router-dom'
import { useAuth } from './useAuth'
import { FullPageSpinner } from '../components/FullPageSpinner'

export function RequireAuth() {
  const { isAuthenticated, isLoading } = useAuth()
  const location = useLocation()

  if (isLoading) return <FullPageSpinner />

  if (!isAuthenticated) {
    const returnTo = location.pathname + location.search
    return <Navigate to={`/login?returnTo=${encodeURIComponent(returnTo)}`} replace />
  }

  return <Outlet />
}
