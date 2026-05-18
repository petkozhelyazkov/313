import { useEffect, useState } from 'react'
import { Outlet, useLocation } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { Sidebar } from '../components/Sidebar'
import { Breadcrumbs } from '../components/Breadcrumbs'
import { ScrollToHash } from '../components/ScrollToHash'
import { CommandPalette } from '../components/CommandPalette'

const authorName = import.meta.env.VITE_APP_AUTHOR_NAME ?? ''
const authorEmail = import.meta.env.VITE_APP_AUTHOR_EMAIL ?? ''
const copyrightYear = import.meta.env.VITE_APP_COPYRIGHT_YEAR ?? new Date().getFullYear().toString()

// Routes where the footer should not render. The dashboard has its own
// scroll behavior and the footer ends up floating mid-page on tall content.
const FOOTERLESS_ROUTES = new Set(['/'])

export function AppLayout() {
  const { t } = useTranslation()
  const location = useLocation()
  const showFooter = !FOOTERLESS_ROUTES.has(location.pathname)
  const [paletteOpen, setPaletteOpen] = useState(false)

  useEffect(() => {
    const handler = (e: KeyboardEvent) => {
      if ((e.metaKey || e.ctrlKey) && e.key.toLowerCase() === 'k') {
        e.preventDefault()
        setPaletteOpen((o) => !o)
      } else if (e.key === 'Escape' && paletteOpen) {
        setPaletteOpen(false)
      }
    }
    window.addEventListener('keydown', handler)
    return () => window.removeEventListener('keydown', handler)
  }, [paletteOpen])

  return (
    <div className="app-shell">
      <ScrollToHash />
      <CommandPalette open={paletteOpen} onClose={() => setPaletteOpen(false)} />
      <Sidebar />
      <div className="app-main">
        <Breadcrumbs />
        <main className="app-content">
          <Outlet />
        </main>
        {showFooter && (
          <footer className="app-footer">
            <div className="app-footer-inner">
              <span>Trading313 · {t('footer.tagline')}</span>
              <span>
                © {copyrightYear}
                {authorName ? ' ' : ''}
                {authorEmail ? (
                  <a href={`mailto:${authorEmail}`} className="app-footer-link">{authorName}</a>
                ) : (
                  authorName
                )}
                {' · MIT'}
              </span>
            </div>
          </footer>
        )}
      </div>
    </div>
  )
}
