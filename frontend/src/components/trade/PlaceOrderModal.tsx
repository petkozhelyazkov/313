import { useEffect, useState } from 'react'
import { Modal, Form, Spinner, Alert } from 'react-bootstrap'
import { useTranslation, Trans } from 'react-i18next'
import { Button, ButtonGroup } from '../Button'
import { useQuote } from '../../api/stocks'
import { usePlaceOrder, type OrderSide } from '../../api/orders'
import { toast } from '../../lib/toast'
import { getApiErrorMessage } from '../../api/client'
import { formatCurrency } from '../../lib/format'

type Props = {
  open: boolean
  onClose: () => void
  symbol: string
}

const SIDES: { value: OrderSide; labelKey: string; helpKey: string }[] = [
  { value: 'LimitBuy', labelKey: 'orders.limitBuy', helpKey: 'orders.helpLimitBuy' },
  { value: 'LimitSell', labelKey: 'orders.limitSell', helpKey: 'orders.helpLimitSell' },
  { value: 'StopLoss', labelKey: 'orders.stopLoss', helpKey: 'orders.helpStopLoss' },
  { value: 'TrailingStop', labelKey: 'trade2.trailingStop', helpKey: 'trade2.helpTrailingStop' },
]

export function PlaceOrderModal({ open, onClose, symbol }: Props) {
  const { t } = useTranslation()
  const [side, setSide] = useState<OrderSide>('LimitBuy')
  const [quantity, setQuantity] = useState<string>('')
  const [limitPrice, setLimitPrice] = useState<string>('')
  const [trailPct, setTrailPct] = useState<string>('5')

  const { data: quote, isLoading: quoteLoading } = useQuote(symbol, open)
  const placeOrder = usePlaceOrder()

  useEffect(() => {
    if (open) {
      setSide('LimitBuy')
      setQuantity('')
      setLimitPrice('')
      setTrailPct('5')
    }
  }, [open, symbol])

  const qtyNum = Number(quantity)
  const priceNum = Number(limitPrice)
  const trailNum = Number(trailPct)
  const isTrailing = side === 'TrailingStop'
  const isValid = isTrailing
    ? Number.isFinite(qtyNum) && qtyNum > 0 && Number.isFinite(trailNum) && trailNum > 0 && trailNum < 100
    : Number.isFinite(qtyNum) && qtyNum > 0 && Number.isFinite(priceNum) && priceNum > 0

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!isValid) return
    try {
      await placeOrder.mutateAsync({
        symbol,
        side,
        quantity: qtyNum,
        limitPrice: isTrailing ? 0 : priceNum,
        trailingStopPercent: isTrailing ? trailNum : undefined,
      })
      const priceForToast = isTrailing && quote ? formatCurrency(quote.price * (1 - trailNum / 100)) : formatCurrency(priceNum)
      toast.success(
        t('toast.orderPlaced', {
          side: t(`orders.${side.charAt(0).toLowerCase() + side.slice(1)}`, { defaultValue: side }),
          qty: qtyNum,
          symbol,
          price: priceForToast,
        }),
      )
      onClose()
    } catch (err) {
      toast.error(getApiErrorMessage(err))
    }
  }

  const sideInfo = SIDES.find((s) => s.value === side)

  return (
    <Modal show={open} onHide={onClose} centered size="lg">
      <Modal.Header closeButton>
        <Modal.Title>{t('orders.placeOrderTitle', { symbol })}</Modal.Title>
      </Modal.Header>
      <Form onSubmit={handleSubmit}>
        <Modal.Body>
          {quoteLoading ? (
            <div className="text-center py-3">
              <Spinner animation="border" size="sm" /> {t('orders.loadingPrice')}
            </div>
          ) : (
            <>
              {quote && (
                <div className="d-flex justify-content-between mb-3">
                  <span className="text-muted">{t('orders.currentPrice')}</span>
                  <strong>{formatCurrency(quote.price)}</strong>
                </div>
              )}

              <Form.Group className="mb-3">
                <Form.Label className="d-block">{t('orders.orderType')}</Form.Label>
                <ButtonGroup>
                  {SIDES.map((s) => (
                    <Button
                      key={s.value}
                      variant="primary"
                      outline={side !== s.value}
                      onClick={() => setSide(s.value)}
                    >
                      {t(s.labelKey)}
                    </Button>
                  ))}
                </ButtonGroup>
                {sideInfo && (
                  <Form.Text className="text-muted d-block mt-2">{t(sideInfo.helpKey)}</Form.Text>
                )}
              </Form.Group>

              <Form.Group className="mb-3" controlId="orderQty">
                <Form.Label>{t('common.quantity')}</Form.Label>
                <Form.Control
                  type="number"
                  inputMode="decimal"
                  step="any"
                  min={0}
                  value={quantity}
                  onChange={(e) => setQuantity(e.target.value)}
                />
              </Form.Group>

              {isTrailing ? (
                <Form.Group className="mb-3" controlId="trailPct">
                  <Form.Label>{t('trade2.trailingPercent')}</Form.Label>
                  <div className="input-group">
                    <Form.Control
                      type="number"
                      inputMode="decimal"
                      step="any"
                      min={0.01}
                      max={99.99}
                      value={trailPct}
                      onChange={(e) => setTrailPct(e.target.value)}
                    />
                    <span className="input-group-text">%</span>
                  </div>
                  <Form.Text className="text-muted">
                    {t('trade2.trailingPercentHint', { pct: trailNum || 0 })}
                  </Form.Text>
                  {quote && Number.isFinite(trailNum) && trailNum > 0 && (
                    <Form.Text className="d-block text-info mt-1">
                      {t('trade2.currentTrigger', { price: formatCurrency(quote.price * (1 - trailNum / 100)) })}
                    </Form.Text>
                  )}
                </Form.Group>
              ) : (
                <Form.Group className="mb-3" controlId="orderPrice">
                  <Form.Label>{t(side === 'StopLoss' ? 'orders.triggerPrice' : 'orders.limitPrice')}</Form.Label>
                  <Form.Control
                    type="number"
                    inputMode="decimal"
                    step="any"
                    min={0}
                    value={limitPrice}
                    onChange={(e) => setLimitPrice(e.target.value)}
                    placeholder={quote ? formatCurrency(quote.price) : '—'}
                  />
                </Form.Group>
              )}

              {isValid && quote && !isTrailing && (
                <Alert variant="light" className="small mb-0">
                  <Trans i18nKey="orders.approxValue" values={{ amount: formatCurrency(qtyNum * priceNum) }} />
                </Alert>
              )}
            </>
          )}
        </Modal.Body>
        <Modal.Footer>
          <Button variant="secondary" outline onClick={onClose} disabled={placeOrder.isPending}>
            {t('common.cancel')}
          </Button>
          <Button type="submit" variant="primary" disabled={!isValid} loading={placeOrder.isPending}>
            {t('orders.placeOrder')}
          </Button>
        </Modal.Footer>
      </Form>
    </Modal>
  )
}
