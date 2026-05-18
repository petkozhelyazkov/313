import { Badge, NavDropdown } from 'react-bootstrap'
import { Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { useAlerts } from '../api/alerts'
import { SymbolLogo } from './SymbolLogo'
import { Icon } from './Icon'
import { formatCurrency } from '../lib/format'

export function AlertsBadge() {
  const { t } = useTranslation()
  const { data } = useAlerts()
  const unread = (data ?? []).filter((a) => a.status === 'Triggered' && !a.acknowledged)

  if (unread.length === 0) {
    return (
      <Link to="/profile#alerts" className="text-decoration-none text-white-50 small" title={t('alerts.noNew')}>
        <Icon name="rr-bell" size={18} />
      </Link>
    )
  }

  return (
    <NavDropdown
      align="end"
      menuVariant="dark"
      title={
        <span className="position-relative">
          <Icon name="sr-bell" size={18} />
          <Badge bg="danger" pill className="position-absolute top-0 start-100 translate-middle">
            {unread.length}
          </Badge>
        </span>
      }
      id="alerts-bell"
    >
      {unread.slice(0, 5).map((a) => (
        <NavDropdown.Item key={a.id} as={Link} to={`/stocks/${encodeURIComponent(a.symbol)}`}>
          <div className="d-flex align-items-center gap-2">
            <SymbolLogo symbol={a.symbol} logoUrl={a.logoUrl} size={22} />
            <div>
              <strong>{a.symbol}</strong>{' '}
              <small className="text-muted">
                {t(a.direction === 'Above' ? 'alerts.above' : 'alerts.below').toLowerCase()} {formatCurrency(a.triggerPrice)}
              </small>
              {a.triggeredPrice != null && (
                <div><small className="text-success">@ {formatCurrency(a.triggeredPrice)}</small></div>
              )}
            </div>
          </div>
        </NavDropdown.Item>
      ))}
      <NavDropdown.Divider />
      <NavDropdown.Item as={Link} to="/profile#alerts">{t('common.viewAll')}</NavDropdown.Item>
    </NavDropdown>
  )
}
