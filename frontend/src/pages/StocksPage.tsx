import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { Container, Card, Row, Col, Spinner } from 'react-bootstrap'
import { useTranslation } from 'react-i18next'
import { useMarketMovers } from '../api/movers'
import { useQuote, useStockDetail } from '../api/stocks'
import { SymbolSearchInput } from '../components/watchlist/SymbolSearchInput'
import { SymbolLogo } from '../components/SymbolLogo'
import { Icon } from '../components/Icon'
import { Link } from 'react-router-dom'
import { useDocumentTitle } from '../hooks/useDocumentTitle'
import { formatCurrency, plClassName, plSignedPercent } from '../lib/format'

const POPULAR = [
  'AAPL', 'MSFT', 'GOOGL', 'AMZN', 'TSLA', 'NVDA', 'META', 'AMD',
  'NFLX', 'DIS', 'INTC', 'CRM',
]

export function StocksPage() {
  const { t } = useTranslation()
  useDocumentTitle(t('nav.stocks'))
  const navigate = useNavigate()
  const { data: movers, isLoading: moversLoading } = useMarketMovers()
  const [recent, setRecent] = useState<string[]>(() => {
    try {
      const raw = localStorage.getItem('trading212.recentSymbols')
      return raw ? (JSON.parse(raw) as string[]) : []
    } catch {
      return []
    }
  })

  const handlePick = (symbol: string) => {
    const sym = symbol.toUpperCase()
    try {
      const updated = [sym, ...recent.filter((s) => s !== sym)].slice(0, 10)
      localStorage.setItem('trading212.recentSymbols', JSON.stringify(updated))
      setRecent(updated)
    } catch {
      /* ignore */
    }
    navigate(`/stocks/${encodeURIComponent(sym)}`)
  }

  return (
    <Container className="py-4">
      <div className="d-flex justify-content-between align-items-center mb-3 flex-wrap gap-2">
        <h1 className="h3 mb-0">{t('stocks.browseTitle')}</h1>
        <span className="text-muted small">{t('stocks.browseSubtitle')}</span>
      </div>

      <Card className="mb-4">
        <Card.Body>
          <Card.Title className="h6 text-muted text-uppercase mb-3">
            <Icon name="rr-search" className="me-2" />
            {t('stocks.searchHeader')}
          </Card.Title>
          <SymbolSearchInput onSelect={handlePick} />
        </Card.Body>
      </Card>

      {recent.length > 0 && (
        <Card className="mb-4">
          <Card.Header>
            <strong><Icon name="rr-clock" className="me-2" /> {t('stocks.recent')}</strong>
          </Card.Header>
          <Card.Body className="p-0">
            <div className="list-group list-group-flush">
              {recent.map((sym) => (
                <SymbolRow key={sym} symbol={sym} />
              ))}
            </div>
          </Card.Body>
        </Card>
      )}

      <Row className="g-3 mb-3">
        <Col lg={6}>
          <Card className="h-100">
            <Card.Header>
              <strong>
                <Icon name="rr-arrow-trend-up" className="me-2 text-success" />
                {t('dashboard.movers.gainers')}
              </strong>
            </Card.Header>
            <Card.Body className="p-0">
              {moversLoading ? (
                <div className="text-center py-4"><Spinner animation="border" size="sm" /></div>
              ) : movers?.gainers.length ? (
                <div className="list-group list-group-flush">
                  {movers.gainers.slice(0, 6).map((m) => (
                    <SymbolRow
                      key={m.symbol}
                      symbol={m.symbol}
                      logoUrl={m.logoUrl}
                      name={m.name}
                      price={m.price}
                      changePct={m.percentChange}
                    />
                  ))}
                </div>
              ) : (
                <div className="text-muted text-center py-3 small">{t('dashboard.movers.noData')}</div>
              )}
            </Card.Body>
          </Card>
        </Col>

        <Col lg={6}>
          <Card className="h-100">
            <Card.Header>
              <strong>
                <Icon name="rr-arrow-trend-down" className="me-2 text-danger" />
                {t('dashboard.movers.losers')}
              </strong>
            </Card.Header>
            <Card.Body className="p-0">
              {moversLoading ? (
                <div className="text-center py-4"><Spinner animation="border" size="sm" /></div>
              ) : movers?.losers.length ? (
                <div className="list-group list-group-flush">
                  {movers.losers.slice(0, 6).map((m) => (
                    <SymbolRow
                      key={m.symbol}
                      symbol={m.symbol}
                      logoUrl={m.logoUrl}
                      name={m.name}
                      price={m.price}
                      changePct={m.percentChange}
                    />
                  ))}
                </div>
              ) : (
                <div className="text-muted text-center py-3 small">{t('dashboard.movers.noData')}</div>
              )}
            </Card.Body>
          </Card>
        </Col>
      </Row>

      <Card>
        <Card.Header>
          <strong><Icon name="sr-bullseye" className="me-2 text-primary" /> {t('stocks.popular')}</strong>
        </Card.Header>
        <Card.Body className="p-0">
          <div className="list-group list-group-flush">
            {POPULAR.map((sym) => (
              <SymbolRow key={sym} symbol={sym} />
            ))}
          </div>
        </Card.Body>
      </Card>
    </Container>
  )
}

function SymbolRow({
  symbol,
  logoUrl,
  name,
  price,
  changePct,
}: {
  symbol: string
  logoUrl?: string | null
  name?: string | null
  price?: number | null
  changePct?: number | null
}) {
  // If price wasn't passed in, fetch it (used for popular + recent lists).
  const shouldFetch = price == null
  const { data: quote } = useQuote(symbol, shouldFetch)
  const { data: detail } = useStockDetail(symbol)

  const resolvedPrice = price ?? quote?.price
  const resolvedPct = changePct ?? quote?.dayChangePct
  const resolvedLogo = logoUrl ?? detail?.stock.logoUrl ?? null
  const resolvedName = name ?? detail?.stock.name ?? null

  return (
    <Link
      to={`/stocks/${encodeURIComponent(symbol)}`}
      className="list-group-item list-group-item-action d-flex justify-content-between align-items-center text-decoration-none"
    >
      <div className="d-flex align-items-center gap-2 min-w-0">
        <SymbolLogo symbol={symbol} logoUrl={resolvedLogo} size={28} />
        <div className="min-w-0">
          <strong>{symbol}</strong>
          {resolvedName && <div className="text-muted small text-truncate">{resolvedName}</div>}
        </div>
      </div>
      <div className="text-end flex-shrink-0 ms-2">
        {resolvedPrice != null ? (
          <>
            <div>{formatCurrency(resolvedPrice)}</div>
            <small className={plClassName(resolvedPct)}>
              {plSignedPercent(resolvedPct)}
            </small>
          </>
        ) : (
          <span className="text-muted small">—</span>
        )}
      </div>
    </Link>
  )
}
