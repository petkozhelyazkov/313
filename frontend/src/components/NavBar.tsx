import { Navbar, Nav, Container, NavDropdown } from 'react-bootstrap'
import { NavLink, useNavigate } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { HealthBadge } from './HealthBadge'
import { LanguageSwitcher } from './LanguageSwitcher'
import { AlertsBadge } from './AlertsBadge'
import { ThemeToggle } from './ThemeToggle'
import { Icon } from './Icon'
import { useAuth } from '../auth/useAuth'

type NavItem = { to: string; labelKey: string; icon: string; adminOnly?: boolean; protected?: boolean }

const navItems: NavItem[] = [
  { to: '/', labelKey: 'nav.dashboard', icon: 'rr-apps', protected: true },
  { to: '/portfolio', labelKey: 'nav.portfolio', icon: 'rr-briefcase', protected: true },
  { to: '/orders', labelKey: 'nav.orders', icon: 'rr-list', protected: true },
  { to: '/watchlist', labelKey: 'nav.watchlist', icon: 'rr-star', protected: true },
  { to: '/analytics', labelKey: 'nav.analytics', icon: 'rr-chart-pie', protected: true },
  { to: '/compare', labelKey: 'nav.compare', icon: 'rr-chart-line-up', protected: true },
  { to: '/admin', labelKey: 'nav.admin', icon: 'rr-shield', adminOnly: true },
]

export function NavBar() {
  const { user, isAuthenticated, logout, hasRole } = useAuth()
  const navigate = useNavigate()
  const { t } = useTranslation()

  const visibleItems = navItems.filter((item) => {
    if (item.adminOnly) return hasRole('Admin')
    if (item.protected) return isAuthenticated
    return true
  })

  return (
    <Navbar bg="dark" variant="dark" expand="lg" sticky="top">
      <Container fluid>
        <Navbar.Brand as={NavLink} to="/">
          Trading313
        </Navbar.Brand>
        <Navbar.Toggle aria-controls="main-nav" />
        <Navbar.Collapse id="main-nav">
          <Nav className="me-auto">
            {visibleItems.map((item) => (
              <Nav.Link
                key={item.to}
                as={NavLink}
                to={item.to}
                end={item.to === '/'}
                className="d-flex align-items-center gap-2"
              >
                <Icon name={item.icon} /> {t(item.labelKey)}
              </Nav.Link>
            ))}
          </Nav>
          <div className="d-flex align-items-center gap-3">
            <HealthBadge />
            {isAuthenticated && <AlertsBadge />}
            <ThemeToggle />
            <LanguageSwitcher />
            {isAuthenticated ? (
              <NavDropdown
                align="end"
                title={user?.displayName || user?.email || 'Account'}
                id="user-menu"
              >
                <NavDropdown.Item as={NavLink} to="/profile">
                  {t('nav.profile')}
                </NavDropdown.Item>
                <NavDropdown.Divider />
                <NavDropdown.Item onClick={() => logout()}>{t('nav.signOut')}</NavDropdown.Item>
              </NavDropdown>
            ) : (
              <>
                <Nav.Link as={NavLink} to="/login">
                  {t('nav.signIn')}
                </Nav.Link>
                <Nav.Link as={NavLink} to="/register" onClick={() => navigate('/register')}>
                  {t('nav.register')}
                </Nav.Link>
              </>
            )}
          </div>
        </Navbar.Collapse>
      </Container>
    </Navbar>
  )
}
