import { Fragment, type ReactNode } from 'react'
import { Link, useLocation, useParams, useSearchParams } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { Icon } from './Icon'

type Crumb = { label: ReactNode; to?: string }

const SEGMENT_LABELS: Record<string, string> = {
  portfolio: 'nav.portfolio',
  orders: 'nav.orders',
  watchlist: 'nav.watchlist',
  analytics: 'nav.analytics',
  compare: 'nav.compare',
  profile: 'nav.profile',
  admin: 'nav.admin',
  stocks: 'breadcrumbs.stocks',
  login: 'nav.signIn',
  register: 'nav.register',
}

/** Page → tab key → human label. Used to add the active tab as the final crumb. */
const TAB_LABELS: Record<string, Record<string, string>> = {
  '/portfolio': { holdings: 'breadcrumbs.holdings', transactions: 'breadcrumbs.transactions' },
  '/orders': { open: 'breadcrumbs.openOrders', history: 'breadcrumbs.history' },
}

export function Breadcrumbs() {
  const location = useLocation()
  const [searchParams] = useSearchParams()
  const params = useParams()
  const { t } = useTranslation()

  const segments = location.pathname.split('/').filter(Boolean)
  if (segments.length === 0) return null // home page has no breadcrumbs

  const crumbs: Crumb[] = [{ label: t('breadcrumbs.home', { defaultValue: 'Home' }), to: '/' }]

  let path = ''
  segments.forEach((seg, i) => {
    path += `/${seg}`
    const isLast = i === segments.length - 1
    const key = SEGMENT_LABELS[seg]
    let label: ReactNode
    if (key) {
      label = t(key, { defaultValue: seg })
    } else if (params.symbol && seg.toLowerCase() === params.symbol.toLowerCase()) {
      label = seg.toUpperCase()
    } else {
      label = decodeURIComponent(seg)
    }
    crumbs.push({ label, to: isLast ? undefined : path })
  })

  const fullPath = '/' + segments.join('/')
  const tabKey = searchParams.get('tab')
  if (tabKey && TAB_LABELS[fullPath]?.[tabKey]) {
    crumbs.push({ label: t(TAB_LABELS[fullPath][tabKey], { defaultValue: tabKey }) })
  }
  const listKey = searchParams.get('list')
  if (listKey && fullPath === '/watchlist') {
    crumbs.push({ label: listKey })
  }

  // Make the last crumb non-clickable even if we computed a "to" for it.
  const lastIdx = crumbs.length - 1

  return (
    <nav aria-label="Breadcrumb" className="breadcrumbs">
      {crumbs.map((c, i) => (
        <Fragment key={i}>
          {i === lastIdx || !c.to ? (
            <span className="breadcrumb-current">{c.label}</span>
          ) : (
            <Link to={c.to} className="breadcrumb-link">
              {c.label}
            </Link>
          )}
          {i < lastIdx && (
            <Icon
              name="rr-angle-right"
              className="breadcrumb-sep"
              style={{ verticalAlign: 'middle' }}
            />
          )}
        </Fragment>
      ))}
    </nav>
  )
}
