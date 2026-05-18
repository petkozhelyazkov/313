import { useMemo } from 'react'
import { Card, Badge } from 'react-bootstrap'
import { useTranslation } from 'react-i18next'
import { plClassName, plSignedCurrency, plSignedPercent } from '../../lib/format'
import { SymbolLogo } from '../SymbolLogo'
import { FiftyTwoWeekRange } from './FiftyTwoWeekRange'
import { LivePrice } from '../LivePrice'
import { useLivePrices } from '../../api/livePrices'
import type { QuoteResponse, StockSearchResult } from '../../api/stocks'

type Props = {
  stock: StockSearchResult
  quote: QuoteResponse | null
  fiftyTwoWeekHigh?: number | null
  fiftyTwoWeekLow?: number | null
}

export function StockHeader({ stock, quote, fiftyTwoWeekHigh, fiftyTwoWeekLow }: Props) {
  const { t } = useTranslation()
  const symbols = useMemo(() => [stock.symbol], [stock.symbol])
  const live = useLivePrices(symbols)
  const liveQuote = live[stock.symbol.toUpperCase()]
  const displayPrice = liveQuote?.price ?? quote?.price ?? null
  const displayChange = liveQuote?.dayChange ?? quote?.dayChange ?? null
  const displayChangePct = liveQuote?.dayChangePct ?? quote?.dayChangePct ?? null
  return (
    <Card className="mb-3">
      <Card.Body>
        <div className="d-flex justify-content-between align-items-start flex-wrap gap-3">
          <div className="d-flex gap-3 align-items-start">
            <SymbolLogo symbol={stock.symbol} logoUrl={stock.logoUrl} size={56} />
            <div>
              <h1 className="h3 mb-1">
                {stock.symbol} <small className="text-muted fs-6">· {stock.name}</small>
              </h1>
              <div className="d-flex gap-2 flex-wrap">
                {stock.exchange && <Badge bg="secondary">{stock.exchange}</Badge>}
                <Badge bg="light" text="dark">{stock.currency}</Badge>
                {stock.country && <Badge bg="light" text="dark">{stock.country}</Badge>}
                {stock.instrumentType && <Badge bg="light" text="dark">{stock.instrumentType}</Badge>}
              </div>
            </div>
          </div>
          <div className="text-end">
            {displayPrice !== null ? (
              <>
                <div className="h2 mb-0">
                  <LivePrice fallbackPrice={displayPrice} liveQuote={liveQuote} />
                </div>
                <div className={plClassName(displayChange)}>
                  {plSignedCurrency(displayChange)} ({plSignedPercent(displayChangePct)})
                </div>
                {quote?.isStale && !liveQuote && (
                  <small className="text-warning">{t('stock.stalePrice')}</small>
                )}
                <FiftyTwoWeekRange
                  current={displayPrice}
                  high={fiftyTwoWeekHigh}
                  low={fiftyTwoWeekLow}
                />
              </>
            ) : (
              <span className="text-muted">{t('stock.priceUnavailable')}</span>
            )}
          </div>
        </div>
      </Card.Body>
    </Card>
  )
}
