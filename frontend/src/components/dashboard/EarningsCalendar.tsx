import { Card, Badge, Spinner } from 'react-bootstrap'
import { Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { useEarningsCalendar } from '../../api/earnings'
import { SymbolLogo } from '../SymbolLogo'

export function EarningsCalendar() {
  const { t } = useTranslation()
  const { data, isLoading, isError } = useEarningsCalendar(14)

  return (
    <Card>
      <Card.Header>
        <strong>{t('dashboard.earningsTitle')}</strong>
      </Card.Header>
      <Card.Body className="p-0">
        {isLoading ? (
          <div className="text-center py-4">
            <Spinner animation="border" size="sm" />
          </div>
        ) : isError ? (
          <div className="text-muted text-center py-4 small">{t('dashboard.movers.couldNotLoad')}</div>
        ) : !data || data.length === 0 ? (
          <div className="text-muted text-center py-4 small">{t('dashboard.earningsEmpty')}</div>
        ) : (
          <div className="list-group list-group-flush">
            {data.slice(0, 8).map((item) => (
              <Link
                key={`${item.symbol}-${item.reportDate}`}
                to={`/stocks/${encodeURIComponent(item.symbol)}`}
                className="list-group-item list-group-item-action d-flex align-items-center gap-2 text-decoration-none"
              >
                <SymbolLogo symbol={item.symbol} logoUrl={item.logoUrl} size={28} />
                <div className="flex-grow-1">
                  <div className="d-flex align-items-center gap-2">
                    <strong>{item.symbol}</strong>
                    {item.isHeld && <Badge bg="success" pill>{t('dashboard.earningsHeld')}</Badge>}
                    {item.isWatched && !item.isHeld && <Badge bg="info" pill>{t('dashboard.earningsWatched')}</Badge>}
                  </div>
                  {item.companyName && (
                    <small className="text-muted">{item.companyName}</small>
                  )}
                </div>
                <div className="text-end">
                  <div className="small">
                    {new Date(item.reportDate).toLocaleDateString(undefined, {
                      month: 'short',
                      day: 'numeric',
                    })}
                  </div>
                  {item.time && <small className="text-muted">{item.time}</small>}
                  {item.epsActual != null ? (
                    <div><small className="text-muted">{t('dashboard.earningsActual')} ${item.epsActual.toFixed(2)}</small></div>
                  ) : item.epsEstimate != null ? (
                    <div><small className="text-muted">{t('dashboard.earningsEstimate')} ${item.epsEstimate.toFixed(2)}</small></div>
                  ) : null}
                </div>
              </Link>
            ))}
          </div>
        )}
      </Card.Body>
    </Card>
  )
}
