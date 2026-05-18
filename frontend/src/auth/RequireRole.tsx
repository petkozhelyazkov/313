import { Navigate, Outlet, useLocation } from 'react-router-dom'
import { Container } from 'react-bootstrap'
import { useAuth } from './useAuth'
import { FullPageSpinner } from '../components/FullPageSpinner'

type Props = { role: string }

export function RequireRole({ role }: Props) {
  const { isAuthenticated, isLoading, hasRole } = useAuth()
  const location = useLocation()

  if (isLoading) return <FullPageSpinner />

  if (!isAuthenticated) {
    const returnTo = location.pathname + location.search
    return <Navigate to={`/login?returnTo=${encodeURIComponent(returnTo)}`} replace />
  }

  if (!hasRole(role)) {
    return (
      <Container className="py-5 text-center">
        <h1 className="display-6">403</h1>
        <p className="lead">You don’t have permission to view this page.</p>
        <p className="text-muted">Required role: {role}</p>
      </Container>
    )
  }

  return <Outlet />
}
