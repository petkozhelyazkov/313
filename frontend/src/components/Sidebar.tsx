import { useEffect, useRef, useState, type ReactNode } from 'react'
import { NavLink, useLocation } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { useAuth } from '../auth/useAuth'
import { useTheme, type Theme } from '../theme/ThemeContext'
import { SUPPORTED_LANGUAGES } from '../i18n'
import { useAlerts } from '../api/alerts'
import { Icon } from './Icon'

const STORAGE_KEY = 'trading212.sidebar.collapsed'

type NavItem = {
  to: string
  labelKey: string
  icon: string
  adminOnly?: boolean
  protected?: boolean
}

const NAV_ITEMS: NavItem[] = [
  { to: '/', labelKey: 'nav.dashboard', icon: 'rr-apps', protected: true },
  { to: '/portfolio', labelKey: 'nav.portfolio', icon: 'rr-briefcase', protected: true },
  { to: '/stocks', labelKey: 'nav.stocks', icon: 'rr-search' },
  { to: '/orders', labelKey: 'nav.orders', icon: 'rr-list', protected: true },
  { to: '/watchlist', labelKey: 'nav.watchlist', icon: 'rr-star', protected: true },
  { to: '/analytics', labelKey: 'nav.analytics', icon: 'rr-chart-pie', protected: true },
  { to: '/compare', labelKey: 'nav.compare', icon: 'rr-chart-line-up', protected: true },
  { to: '/admin', labelKey: 'nav.admin', icon: 'rr-shield', adminOnly: true },
]

const THEME_OPTIONS: { value: Theme; icon: string; labelKey: string }[] = [
  { value: 'light', icon: 'rr-sun', labelKey: 'sidebar.theme.light' },
  { value: 'dark', icon: 'rr-moon', labelKey: 'sidebar.theme.dark' },
  { value: 'system', icon: 'rr-computer', labelKey: 'sidebar.theme.system' },
]

export function Sidebar() {
  const [collapsed, setCollapsed] = useState<boolean>(() => {
    try {
      return localStorage.getItem(STORAGE_KEY) === 'true'
    } catch {
      return false
    }
  })
  const [mobileOpen, setMobileOpen] = useState(false)
  const [userMenuOpen, setUserMenuOpen] = useState(false)
  const [themeMenuOpen, setThemeMenuOpen] = useState(false)
  const [langMenuOpen, setLangMenuOpen] = useState(false)

  const footerRef = useRef<HTMLDivElement>(null)
  const { user, isAuthenticated, hasRole, logout } = useAuth()
  const { theme, setTheme } = useTheme()
  const { t, i18n } = useTranslation()
  const location = useLocation()
  const { data: alerts } = useAlerts()
  const unreadAlerts = (alerts ?? []).filter((a) => a.status === 'Triggered' && !a.acknowledged).length

  useEffect(() => {
    try {
      localStorage.setItem(STORAGE_KEY, collapsed ? 'true' : 'false')
    } catch {
      /* ignore */
    }
  }, [collapsed])

  // Auto-close mobile drawer + sub-menus on navigation
  useEffect(() => {
    setMobileOpen(false)
    setUserMenuOpen(false)
    setThemeMenuOpen(false)
    setLangMenuOpen(false)
  }, [location.pathname])

  // Close submenus when clicking outside the footer or pressing Escape
  useEffect(() => {
    if (!userMenuOpen && !themeMenuOpen && !langMenuOpen) return

    const handlePointerDown = (e: MouseEvent | TouchEvent) => {
      const target = e.target as Node | null
      if (footerRef.current && target && !footerRef.current.contains(target)) {
        setUserMenuOpen(false)
        setThemeMenuOpen(false)
        setLangMenuOpen(false)
      }
    }
    const handleKey = (e: KeyboardEvent) => {
      if (e.key === 'Escape') {
        setUserMenuOpen(false)
        setThemeMenuOpen(false)
        setLangMenuOpen(false)
      }
    }

    document.addEventListener('mousedown', handlePointerDown)
    document.addEventListener('touchstart', handlePointerDown)
    document.addEventListener('keydown', handleKey)
    return () => {
      document.removeEventListener('mousedown', handlePointerDown)
      document.removeEventListener('touchstart', handlePointerDown)
      document.removeEventListener('keydown', handleKey)
    }
  }, [userMenuOpen, themeMenuOpen, langMenuOpen])

  const visibleItems = NAV_ITEMS.filter((item) => {
    if (item.adminOnly) return hasRole('Admin')
    if (item.protected) return isAuthenticated
    return true
  })

  const currentLang = i18n.resolvedLanguage ?? i18n.language ?? 'en'
  const currentLangLabel = SUPPORTED_LANGUAGES.find((l) => l.code === currentLang)?.label ?? 'English'
  const currentThemeIcon = THEME_OPTIONS.find((o) => o.value === theme)?.icon ?? 'rr-computer'

  const displayName = user?.displayName || user?.email || 'Account'
  const avatarLetter = (displayName[0] || '?').toUpperCase()

  const closeAllMenus = () => {
    setUserMenuOpen(false)
    setThemeMenuOpen(false)
    setLangMenuOpen(false)
  }

  return (
    <>
      <button
        type="button"
        className="sidebar-mobile-toggle"
        onClick={() => setMobileOpen(true)}
        aria-label="Open menu"
      >
        <Icon name="rr-menu-burger" size={20} />
      </button>

      {mobileOpen && (
        <div
          className="sidebar-backdrop d-lg-none"
          onClick={() => setMobileOpen(false)}
          aria-hidden
        />
      )}

      <aside
        className={[
          'sidebar',
          collapsed ? 'sidebar-collapsed' : '',
          mobileOpen ? 'sidebar-mobile-open' : '',
        ].filter(Boolean).join(' ')}
      >
        <div className="sidebar-brand">
          <NavLink to="/" className="sidebar-brand-link text-decoration-none">
            <img src="/logo.svg" alt="Trading313" className="sidebar-brand-logo" />
            <span className="sidebar-brand-text">Trading313</span>
          </NavLink>
          <button
            type="button"
            className="sidebar-toggle-btn d-none d-lg-inline-flex"
            onClick={() => {
              closeAllMenus()
              setCollapsed((c) => !c)
            }}
            aria-label={collapsed ? 'Expand sidebar' : 'Collapse sidebar'}
            title={collapsed ? 'Expand' : 'Collapse'}
          >
            <Icon name={collapsed ? 'rr-angle-right' : 'rr-angle-left'} size={14} />
          </button>
          <button
            type="button"
            className="sidebar-toggle-btn d-lg-none"
            onClick={() => setMobileOpen(false)}
            aria-label="Close menu"
          >
            <Icon name="rr-angle-left" size={14} />
          </button>
        </div>

        <nav className="sidebar-nav">
          {visibleItems.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              end={item.to === '/'}
              className={({ isActive }) => `sidebar-link${isActive ? ' active' : ''}`}
              title={collapsed ? t(item.labelKey) : undefined}
            >
              <Icon name={item.icon} className="sidebar-link-icon" />
              <span className="sidebar-link-label">{t(item.labelKey)}</span>
              {item.to === '/profile' && unreadAlerts > 0 && (
                <span className="badge bg-danger sidebar-link-meta">{unreadAlerts}</span>
              )}
            </NavLink>
          ))}
        </nav>

        <div className="sidebar-footer" ref={footerRef}>
          {isAuthenticated && (
            <NavLink
              to="/profile#alerts"
              className={({ isActive }) => `sidebar-link${isActive ? ' active' : ''}`}
              title={collapsed ? 'Alerts' : undefined}
            >
              <Icon name={unreadAlerts > 0 ? 'sr-bell' : 'rr-bell'} className="sidebar-link-icon" />
              <span className="sidebar-link-label">{t('sidebar.alerts')}</span>
              {unreadAlerts > 0 && (
                <span className="badge bg-danger sidebar-link-meta">{unreadAlerts}</span>
              )}
            </NavLink>
          )}

          <SidebarPopoverGroup>
            <SidebarTrigger
              icon={currentThemeIcon}
              label={t(`sidebar.theme.${theme}`, { defaultValue: theme[0].toUpperCase() + theme.slice(1) })}
              open={themeMenuOpen}
              onToggle={() => {
                setThemeMenuOpen((o) => !o)
                setUserMenuOpen(false)
                setLangMenuOpen(false)
              }}
              collapsed={collapsed}
            />
            <SidebarMenu open={themeMenuOpen}>
              {THEME_OPTIONS.map((opt) => (
                <button
                  key={opt.value}
                  type="button"
                  className={`sidebar-link${theme === opt.value ? ' active' : ''}`}
                  onClick={() => {
                    setTheme(opt.value)
                    setThemeMenuOpen(false)
                  }}
                >
                  <Icon name={opt.icon} className="sidebar-link-icon" />
                  <span className="sidebar-link-label">
                    {t(opt.labelKey, { defaultValue: opt.value[0].toUpperCase() + opt.value.slice(1) })}
                  </span>
                </button>
              ))}
            </SidebarMenu>
          </SidebarPopoverGroup>

          <SidebarPopoverGroup>
            <button
              type="button"
              className={`sidebar-link sidebar-user-trigger${langMenuOpen ? ' open' : ''}`}
              onClick={() => {
                setLangMenuOpen((o) => !o)
                setUserMenuOpen(false)
                setThemeMenuOpen(false)
              }}
              title={collapsed ? currentLangLabel : undefined}
            >
              <span className="sidebar-lang-code">{currentLang.toUpperCase()}</span>
              <span className="sidebar-link-label">{currentLangLabel}</span>
              <Icon name="rr-angle-up" className="sidebar-chevron" />
            </button>
            <SidebarMenu open={langMenuOpen}>
              {SUPPORTED_LANGUAGES.map((lang) => (
                <button
                  key={lang.code}
                  type="button"
                  className={`sidebar-link${lang.code === currentLang ? ' active' : ''}`}
                  onClick={() => {
                    i18n.changeLanguage(lang.code)
                    setLangMenuOpen(false)
                  }}
                >
                  <span className="sidebar-lang-code">{lang.code.toUpperCase()}</span>
                  <span className="sidebar-link-label">{lang.label}</span>
                </button>
              ))}
            </SidebarMenu>
          </SidebarPopoverGroup>

          {isAuthenticated ? (
            <SidebarPopoverGroup>
              <button
                type="button"
                className={`sidebar-link sidebar-user-trigger${userMenuOpen ? ' open' : ''}`}
                onClick={() => {
                  setUserMenuOpen((o) => !o)
                  setThemeMenuOpen(false)
                  setLangMenuOpen(false)
                }}
                title={collapsed ? displayName : undefined}
              >
                <span className="sidebar-user-avatar">{avatarLetter}</span>
                <span className="sidebar-link-label">{displayName}</span>
                <Icon name="rr-angle-up" className="sidebar-chevron" />
              </button>
              <SidebarMenu open={userMenuOpen}>
                <NavLink
                  to="/profile"
                  className={({ isActive }) => `sidebar-link${isActive ? ' active' : ''}`}
                  onClick={() => setUserMenuOpen(false)}
                >
                  <Icon name="rr-user" className="sidebar-link-icon" />
                  <span className="sidebar-link-label">{t('nav.profile')}</span>
                </NavLink>
                <button
                  type="button"
                  className="sidebar-link text-danger"
                  onClick={() => {
                    setUserMenuOpen(false)
                    logout()
                  }}
                >
                  <Icon name="rr-sign-out-alt" className="sidebar-link-icon" />
                  <span className="sidebar-link-label">{t('nav.signOut')}</span>
                </button>
              </SidebarMenu>
            </SidebarPopoverGroup>
          ) : (
            <>
              <NavLink to="/login" className="sidebar-link" title={collapsed ? t('nav.signIn') : undefined}>
                <Icon name="rr-sign-out-alt" className="sidebar-link-icon" />
                <span className="sidebar-link-label">{t('nav.signIn')}</span>
              </NavLink>
              <NavLink to="/register" className="sidebar-link" title={collapsed ? t('nav.register') : undefined}>
                <Icon name="rr-user" className="sidebar-link-icon" />
                <span className="sidebar-link-label">{t('nav.register')}</span>
              </NavLink>
            </>
          )}
        </div>
      </aside>
    </>
  )
}

function SidebarPopoverGroup({ children }: { children: ReactNode }) {
  return <div className="position-relative">{children}</div>
}

function SidebarTrigger({
  icon, label, open, onToggle, collapsed,
}: {
  icon: string
  label: string
  open: boolean
  onToggle: () => void
  collapsed: boolean
}) {
  return (
    <button
      type="button"
      className={`sidebar-link sidebar-user-trigger${open ? ' open' : ''}`}
      onClick={onToggle}
      title={collapsed ? label : undefined}
    >
      <Icon name={icon} className="sidebar-link-icon" />
      <span className="sidebar-link-label">{label}</span>
      <Icon name="rr-angle-up" className="sidebar-chevron" />
    </button>
  )
}

function SidebarMenu({ open, children }: { open: boolean; children: ReactNode }) {
  return (
    <div className={`sidebar-user-menu${open ? ' open' : ''}`} aria-hidden={!open}>
      {children}
    </div>
  )
}
