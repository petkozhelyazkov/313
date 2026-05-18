import { Card, Spinner } from 'react-bootstrap'
import { Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { useQuote, useStockDetail } from '../../api/stocks'
import { formatCurrency, plClassName, plSignedPercent } from '../../lib/format'
import { SymbolLogo } from '../SymbolLogo'

const HOTLIST_SYMBOLS = ['AAPL', 'MSFT', 'GOOGL', 'AMZN', 'TSLA', 'NVDA', 'META', 'AMD']

export function Hotlist() {
  const { t } = useTranslation()
  return (
    <Card>
      <Card.Header>
        <strong>{t('dashboard.popularStocks')}</strong>
      </Card.Header>
      <Card.Body className="p-0">
        <div className="list-group list-group-flush">
          {HOTLIST_SYMBOLS.map((symbol) => (
            <HotlistRow key={symbol} symbol={symbol} />
          ))}
        </div>
      </Card.Body>
    </Card>
  )
}

function HotlistRow({ symbol }: { symbol: string }) {
  const { t } = useTranslation()
  const { data: quote, isLoading, isError } = useQuote(symbol)
  // Fetch metadata for the logo. useStockDetail uses the composite endpoint
  // which is cached aggressively — usually a single API call per symbol ever.
  const { data: detail } = useStockDetail(symbol)
  const logoUrl = detail?.stock.logoUrl ?? null

  return (
    <Link
      to={`/stocks/${encodeURIComponent(symbol)}`}
      className="list-group-item list-group-item-action d-flex justify-content-between align-items-center text-decoration-none"
    >
      <div className="d-flex align-items-center gap-2">
        <SymbolLogo symbol={symbol} logoUrl={logoUrl} size={28} />
        <strong>{symbol}</strong>
      </div>
      <div className="text-end">
        {isLoading ? (
          <Spinner animation="border" size="sm" />
        ) : isError || !quote ? (
          <span className="text-muted small">{t('dashboard.unavailable')}</span>
        ) : (
          <>
            <div>{formatCurrency(quote.price)}</div>
            <small className={plClassName(quote.dayChangePct)}>
              {plSignedPercent(quote.dayChangePct)}
            </small>
          </>
        )}
      </div>
    </Link>
  )
}
