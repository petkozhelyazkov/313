import { Card, Tab, Tabs, Spinner } from 'react-bootstrap'
import { Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { useMarketMovers, type MoverItem } from '../../api/movers'
import { SymbolLogo } from '../SymbolLogo'
import { Icon } from '../Icon'
import { formatCurrency, plClassName, plSignedPercent } from '../../lib/format'

export function MarketMovers() {
  const { t } = useTranslation()
  const { data, isLoading, isError } = useMarketMovers()

  return (
    <Card>
      <Card.Header>
        <strong>{t('dashboard.movers.title')}</strong>
      </Card.Header>
      <Card.Body className="p-0">
        {isLoading ? (
          <div className="text-center py-4"><Spinner animation="border" size="sm" /></div>
        ) : isError || !data ? (
          <div className="text-muted text-center py-4 small">{t('dashboard.movers.couldNotLoad')}</div>
        ) : (
          <Tabs defaultActiveKey="gainers" className="px-3 pt-2" justify>
            <Tab eventKey="gainers" title={<><Icon name="rr-arrow-trend-up" className="me-1 text-success" />{t('dashboard.movers.gainers')}</>}>
              <List items={data.gainers} />
            </Tab>
            <Tab eventKey="losers" title={<><Icon name="rr-arrow-trend-down" className="me-1 text-danger" />{t('dashboard.movers.losers')}</>}>
              <List items={data.losers} />
            </Tab>
            <Tab eventKey="actives" title={<><Icon name="rr-fire" className="me-1 text-warning" />{t('dashboard.movers.active')}</>}>
              <List items={data.actives} />
            </Tab>
          </Tabs>
        )}
      </Card.Body>
    </Card>
  )
}

function List({ items }: { items: MoverItem[] }) {
  const { t } = useTranslation()
  if (items.length === 0) {
    return <div className="text-muted text-center py-3 small">{t('dashboard.movers.noData')}</div>
  }
  return (
    <div className="list-group list-group-flush">
      {items.map((item) => (
        <Link
          key={item.symbol}
          to={`/stocks/${encodeURIComponent(item.symbol)}`}
          className="list-group-item list-group-item-action d-flex align-items-center gap-2 text-decoration-none"
        >
          <SymbolLogo symbol={item.symbol} logoUrl={item.logoUrl} size={26} />
          <div className="flex-grow-1">
            <strong>{item.symbol}</strong>
            {item.name && <div><small className="text-muted">{item.name}</small></div>}
          </div>
          <div className="text-end">
            <div>{formatCurrency(item.price)}</div>
            <small className={plClassName(item.percentChange)}>
              {plSignedPercent(item.percentChange)}
            </small>
          </div>
        </Link>
      ))}
    </div>
  )
}
