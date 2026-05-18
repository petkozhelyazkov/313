import { useState } from 'react'
import { Container, Tabs, Tab, Card, Spinner } from 'react-bootstrap'
import { useTranslation } from 'react-i18next'
import { usePortfolioSummary } from '../api/portfolio'
import { HoldingsTable } from '../components/portfolio/HoldingsTable'
import { TransactionTable } from '../components/portfolio/TransactionTable'
import { TradeModal } from '../components/trade/TradeModal'
import { useTabFromUrl } from '../hooks/useTabFromUrl'
import { useDocumentTitle } from '../hooks/useDocumentTitle'
import { formatCurrency, plClassName, plSignedCurrency } from '../lib/format'

type TradeState = { symbol: string; mode: 'buy' | 'sell' } | null

export function PortfolioPage() {
  const { t } = useTranslation()
  useDocumentTitle(t('portfolio.title'))
  const { data: summary, isLoading } = usePortfolioSummary()
  const [trade, setTrade] = useState<TradeState>(null)
  const [activeTab, setActiveTab] = useTabFromUrl('tab', 'holdings')

  const handleTrade = (symbol: string, mode: 'buy' | 'sell') => setTrade({ symbol, mode })

  return (
    <Container className="py-4">
      <div className="d-flex justify-content-between align-items-center mb-3">
        <h1 className="h3 mb-0">{t('portfolio.title')}</h1>
        {summary && (
          <div className="text-end">
            <div className="small text-muted">{t('dashboard.freeFunds')}</div>
            <strong>{formatCurrency(summary.cashBalance)}</strong>
          </div>
        )}
      </div>

      {summary && (
        <Card className="mb-3">
          <Card.Body className="d-flex flex-wrap gap-4">
            <div>
              <div className="text-muted small text-uppercase">{t('dashboard.totalValue')}</div>
              <strong className="fs-5">{formatCurrency(summary.totalValue)}</strong>
            </div>
            <div>
              <div className="text-muted small text-uppercase">{t('portfolio.holdings')}</div>
              <strong className="fs-5">{formatCurrency(summary.holdingsValue)}</strong>
            </div>
            <div>
              <div className="text-muted small text-uppercase">{t('dashboard.invested')}</div>
              <strong className="fs-5">{formatCurrency(summary.totalInvested)}</strong>
            </div>
            <div>
              <div className="text-muted small text-uppercase">{t('portfolio.unrealizedPl')}</div>
              <strong className={`fs-5 ${plClassName(summary.unrealizedPl)}`}>
                {plSignedCurrency(summary.unrealizedPl)}
              </strong>
            </div>
            <div>
              <div className="text-muted small text-uppercase">{t('portfolio.realizedLifetime')}</div>
              <strong className={`fs-5 ${plClassName(summary.realizedPlLifetime)}`}>
                {plSignedCurrency(summary.realizedPlLifetime)}
              </strong>
            </div>
          </Card.Body>
        </Card>
      )}

      <Tabs activeKey={activeTab} onSelect={(k) => k && setActiveTab(k)} className="mb-3">
        <Tab eventKey="holdings" title={t('portfolio.holdings')}>
          {isLoading || !summary ? (
            <Card body className="text-center">
              <Spinner animation="border" size="sm" /> {t('common.loading')}
            </Card>
          ) : (
            <HoldingsTable
              positions={summary.positions.filter((p) => !p.isClosed)}
              onTrade={handleTrade}
            />
          )}
        </Tab>
        <Tab eventKey="transactions" title={t('portfolio.transactions')}>
          <TransactionTable />
        </Tab>
      </Tabs>

      {trade && (
        <TradeModal
          open={trade !== null}
          onClose={() => setTrade(null)}
          symbol={trade.symbol}
          mode={trade.mode}
        />
      )}
    </Container>
  )
}
