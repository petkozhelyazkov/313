import { useState } from 'react'
import { Container, Card, Spinner, OverlayTrigger, Tooltip } from 'react-bootstrap'
import { useTranslation, Trans } from 'react-i18next'
import { Button, ButtonGroup } from '../components/Button'
import { Link, useParams } from 'react-router-dom'
import { useStockDetail, type Range } from '../api/stocks'
import { useStockProfile } from '../api/profile'
import { useAuth } from '../auth/useAuth'
import { useAddToWatchlist, useRemoveFromWatchlist } from '../api/watchlist'
import { StockHeader } from '../components/stocks/StockHeader'
import { PriceChart } from '../components/stocks/PriceChart'
import { YourPositionCard } from '../components/stocks/YourPositionCard'
import { CompanyProfileCard } from '../components/stocks/CompanyProfileCard'
import { DividendHistoryCard } from '../components/stocks/DividendHistoryCard'
import { SplitHistoryCard } from '../components/stocks/SplitHistoryCard'
import { AnalystConsensusCard } from '../components/stocks/AnalystConsensusCard'
import { InsiderTradesCard } from '../components/stocks/InsiderTradesCard'
import { TradeModal } from '../components/trade/TradeModal'
import { PlaceOrderModal } from '../components/trade/PlaceOrderModal'
import { CreateAlertModal } from '../components/trade/CreateAlertModal'
import { Icon } from '../components/Icon'
import { useDocumentTitle } from '../hooks/useDocumentTitle'
import { toast } from '../lib/toast'
import { getApiErrorMessage } from '../api/client'

export function StockDetailPage() {
  const { t } = useTranslation()
  const { symbol = '' } = useParams<{ symbol: string }>()
  const upperSymbol = symbol.toUpperCase()
  useDocumentTitle(upperSymbol)
  const { isAuthenticated } = useAuth()
  const { data, isLoading, isError } = useStockDetail(upperSymbol)
  const { data: profile } = useStockProfile(upperSymbol)
  const [range, setRange] = useState<Range>('1Y')
  const [trade, setTrade] = useState<{ mode: 'buy' | 'sell' } | null>(null)
  const [orderOpen, setOrderOpen] = useState(false)
  const [alertOpen, setAlertOpen] = useState(false)
  const addWatchlist = useAddToWatchlist()
  const removeWatchlist = useRemoveFromWatchlist()
  const watchlistPending = addWatchlist.isPending || removeWatchlist.isPending

  if (isLoading) {
    return (
      <Container className="py-5 text-center">
        <Spinner animation="border" /> {t('common.loading')}
      </Container>
    )
  }

  if (isError || !data) {
    return (
      <Container className="py-5">
        <Card body className="text-center">
          <p className="mb-2"><Trans i18nKey="stock.couldNotLoad" values={{ symbol: upperSymbol }} /></p>
          <p className="text-muted">{t('stock.couldNotLoadHint')}</p>
        </Card>
      </Container>
    )
  }

  const canTrade = isAuthenticated && data.quote != null

  const tradeButton = (mode: 'buy' | 'sell') => {
    const btn = (
      <Button
        variant={mode === 'buy' ? 'success' : 'danger'}
        disabled={!canTrade}
        onClick={() => setTrade({ mode })}
      >
        {t(mode === 'buy' ? 'common.buy' : 'common.sell')}
      </Button>
    )
    if (canTrade) return btn
    return (
      <OverlayTrigger overlay={<Tooltip>{isAuthenticated ? t('stock.priceUnavailable') : t('stock.signInToTrade')}</Tooltip>}>
        <span className="d-inline-block">{btn}</span>
      </OverlayTrigger>
    )
  }

  return (
    <Container className="py-4">
      <div className="mb-2 small">
        <Link to="/portfolio" className="text-decoration-none">{t('stock.backToPortfolio')}</Link>
      </div>

      <StockHeader
        stock={data.stock}
        quote={data.quote}
        fiftyTwoWeekHigh={profile?.fiftyTwoWeekHigh}
        fiftyTwoWeekLow={profile?.fiftyTwoWeekLow}
      />

      <div className="d-flex justify-content-end mb-3 gap-2">
        <ButtonGroup>
          {tradeButton('buy')}
          {tradeButton('sell')}
        </ButtonGroup>
        <Button
          variant="primary"
          outline
          disabled={!isAuthenticated}
          onClick={() => setOrderOpen(true)}
        >
          {t('stock.placeOrder')}
        </Button>
        <Button
          variant="primary"
          outline
          disabled={!isAuthenticated}
          onClick={() => setAlertOpen(true)}
          iconLeft={<Icon name="rr-bell" />}
        >
          {t('stock.alert')}
        </Button>
        <Button
          variant={data.inWatchlist ? 'warning' : 'primary'}
          outline={!data.inWatchlist}
          disabled={!isAuthenticated || watchlistPending}
          onClick={async () => {
            try {
              if (data.inWatchlist) {
                await removeWatchlist.mutateAsync({ symbol: upperSymbol })
                toast.success(t('toast.removedFromWatchlist', { symbol: upperSymbol }))
              } else {
                await addWatchlist.mutateAsync({ symbol: upperSymbol })
                toast.success(t('toast.addedToWatchlist', { symbol: upperSymbol }))
              }
            } catch (err) {
              toast.error(getApiErrorMessage(err))
            }
          }}
        >
          <Icon name={data.inWatchlist ? 'sr-star' : 'rr-star'} className="me-1" />{t(data.inWatchlist ? 'stock.inWatchlist' : 'stock.addToWatchlist')}
        </Button>
      </div>

      {data.userPosition && <YourPositionCard position={data.userPosition} />}

      <PriceChart
        symbol={upperSymbol}
        range={range}
        onRangeChange={setRange}
        averageCost={data.userPosition?.averageCost}
      />

      {profile && <CompanyProfileCard profile={profile} />}

      <AnalystConsensusCard symbol={upperSymbol} />

      <InsiderTradesCard symbol={upperSymbol} />

      <DividendHistoryCard symbol={upperSymbol} />

      <SplitHistoryCard symbol={upperSymbol} />

      <PlaceOrderModal
        open={orderOpen}
        onClose={() => setOrderOpen(false)}
        symbol={upperSymbol}
      />

      <CreateAlertModal
        open={alertOpen}
        onClose={() => setAlertOpen(false)}
        symbol={upperSymbol}
      />

      {trade && (
        <TradeModal
          open={trade !== null}
          onClose={() => setTrade(null)}
          symbol={upperSymbol}
          mode={trade.mode}
        />
      )}
    </Container>
  )
}
