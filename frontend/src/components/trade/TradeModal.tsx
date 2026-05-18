import { useEffect, useState } from 'react'
import { Modal, Form, Spinner, Alert } from 'react-bootstrap'
import { useTranslation } from 'react-i18next'
import { Button } from '../Button'
import { useBuyMutation, useSellMutation, usePortfolioSummary } from '../../api/portfolio'
import { useQuote } from '../../api/stocks'
import { toast } from '../../lib/toast'
import { getApiErrorMessage } from '../../api/client'
import { formatCurrency, formatQty } from '../../lib/format'

type Props = {
  open: boolean
  onClose: () => void
  symbol: string
  mode: 'buy' | 'sell'
}

export function TradeModal({ open, onClose, symbol, mode }: Props) {
  const { t } = useTranslation()
  const [quantity, setQuantity] = useState<string>('')
  const { data: quote, isLoading: quoteLoading } = useQuote(symbol, open)
  const { data: summary } = usePortfolioSummary()
  const buy = useBuyMutation()
  const sell = useSellMutation()
  const mutation = mode === 'buy' ? buy : sell

  useEffect(() => {
    if (open) {
      setQuantity('')
      mutation.reset()
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open, symbol, mode])

  const qtyNum = Number(quantity)
  const isValidQty = quantity !== '' && Number.isFinite(qtyNum) && qtyNum > 0

  const cash = summary?.cashBalance ?? 0
  const position = summary?.positions.find((p) => p.symbol.toUpperCase() === symbol.toUpperCase())
  const heldQty = position?.quantity ?? 0

  const price = quote?.price ?? 0
  const totalCost = isValidQty ? qtyNum * price : 0

  const maxBuy = price > 0 ? Math.floor((cash / price) * 1e6) / 1e6 : 0
  const maxQty = mode === 'buy' ? maxBuy : heldQty
  const overMax = isValidQty && qtyNum > maxQty

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!isValidQty || overMax || quoteLoading) return

    try {
      const action = mode === 'buy' ? buy : sell
      await action.mutateAsync({ symbol, quantity: qtyNum })
      toast.success(t(mode === 'buy' ? 'toast.bought' : 'toast.sold', { qty: formatQty(qtyNum), symbol }))
      onClose()
    } catch (err) {
      toast.error(getApiErrorMessage(err))
    }
  }

  return (
    <Modal show={open} onHide={onClose} centered>
      <Modal.Header closeButton>
        <Modal.Title>{t(mode === 'buy' ? 'trade.buyTitle' : 'trade.sellTitle', { symbol })}</Modal.Title>
      </Modal.Header>
      <Form onSubmit={handleSubmit}>
        <Modal.Body>
          {quoteLoading ? (
            <div className="text-center py-3">
              <Spinner animation="border" size="sm" /> {t('trade.loadingPrice')}
            </div>
          ) : !quote ? (
            <Alert variant="danger">{t('trade.couldNotLoadPrice', { symbol })}</Alert>
          ) : (
            <>
              <div className="d-flex justify-content-between mb-3">
                <span className="text-muted">{t('trade.currentPrice')}</span>
                <strong>{formatCurrency(quote.price)}</strong>
              </div>

              {mode === 'buy' ? (
                <div className="d-flex justify-content-between mb-3">
                  <span className="text-muted">{t('trade.availableCash')}</span>
                  <strong>{formatCurrency(cash)}</strong>
                </div>
              ) : (
                <div className="d-flex justify-content-between mb-3">
                  <span className="text-muted">{t('trade.sharesHeld')}</span>
                  <strong>{formatQty(heldQty)}</strong>
                </div>
              )}

              <Form.Group controlId="tradeQuantity" className="mb-3">
                <Form.Label>{t('common.quantity')}</Form.Label>
                <Form.Control
                  type="number"
                  inputMode="decimal"
                  step="any"
                  min={0}
                  value={quantity}
                  onChange={(e) => setQuantity(e.target.value)}
                  placeholder={t('trade.upTo', { qty: formatQty(mode === 'buy' ? maxBuy : heldQty) })}
                  isInvalid={overMax}
                  autoFocus
                />
                <Form.Control.Feedback type="invalid">
                  {mode === 'buy'
                    ? t('trade.maxBuyHint', { qty: formatQty(maxBuy) })
                    : t('trade.maxSellHint', { qty: formatQty(heldQty) })}
                </Form.Control.Feedback>
                <Form.Text className="text-muted">{t('trade.quantityHint')}</Form.Text>
              </Form.Group>

              <hr />
              <div className="d-flex justify-content-between">
                <span className="text-muted">
                  {t(mode === 'buy' ? 'trade.estimatedCost' : 'trade.estimatedProceeds')}
                </span>
                <strong>{isValidQty ? formatCurrency(totalCost) : '—'}</strong>
              </div>
              {mode === 'buy' && isValidQty && !overMax && (
                <div className="d-flex justify-content-between small text-muted mt-1">
                  <span>{t('trade.cashAfter')}</span>
                  <span>{formatCurrency(cash - totalCost)}</span>
                </div>
              )}

              {quote.isStale && (
                <Alert variant="warning" className="mt-3 mb-0 py-2">{t('trade.staleWarning')}</Alert>
              )}
            </>
          )}
        </Modal.Body>
        <Modal.Footer>
          <Button variant="secondary" outline onClick={onClose} disabled={mutation.isPending}>
            {t('common.cancel')}
          </Button>
          <Button
            type="submit"
            variant={mode === 'buy' ? 'success' : 'danger'}
            disabled={!isValidQty || overMax || quoteLoading}
            loading={mutation.isPending}
          >
            {t(mode === 'buy' ? 'trade.confirmBuy' : 'trade.confirmSell')}
          </Button>
        </Modal.Footer>
      </Form>
    </Modal>
  )
}
