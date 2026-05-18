import { useEffect, useState } from 'react'
import { Modal, Form } from 'react-bootstrap'
import { useTranslation } from 'react-i18next'
import { Button, ButtonGroup } from '../Button'
import { useQuote } from '../../api/stocks'
import { useCreateAlert, type AlertDirection } from '../../api/alerts'
import { toast } from '../../lib/toast'
import { getApiErrorMessage } from '../../api/client'
import { formatCurrency } from '../../lib/format'

type Props = {
  open: boolean
  onClose: () => void
  symbol: string
}

export function CreateAlertModal({ open, onClose, symbol }: Props) {
  const { t } = useTranslation()
  const { data: quote } = useQuote(symbol, open)
  const [direction, setDirection] = useState<AlertDirection>('Above')
  const [trigger, setTrigger] = useState('')
  const create = useCreateAlert()

  useEffect(() => {
    if (open) {
      setDirection('Above')
      setTrigger('')
    }
  }, [open, symbol])

  const triggerNum = Number(trigger)
  const isValid = trigger !== '' && Number.isFinite(triggerNum) && triggerNum > 0

  const submit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!isValid) return
    try {
      await create.mutateAsync({ symbol, direction, triggerPrice: triggerNum })
      toast.success(
        t('toast.alertSet', {
          symbol,
          direction: t(direction === 'Above' ? 'alerts.above' : 'alerts.below').toLowerCase(),
          price: formatCurrency(triggerNum),
        }),
      )
      onClose()
    } catch (err) {
      toast.error(getApiErrorMessage(err))
    }
  }

  return (
    <Modal show={open} onHide={onClose} centered>
      <Modal.Header closeButton>
        <Modal.Title>{t('alerts.setTitle', { symbol })}</Modal.Title>
      </Modal.Header>
      <Form onSubmit={submit}>
        <Modal.Body>
          {quote && (
            <div className="d-flex justify-content-between mb-3">
              <span className="text-muted">{t('trade.currentPrice')}</span>
              <strong>{formatCurrency(quote.price)}</strong>
            </div>
          )}
          <Form.Group className="mb-3">
            <Form.Label className="d-block">{t('alerts.notifyWhen')}</Form.Label>
            <ButtonGroup>
              <Button
                variant="success"
                outline={direction !== 'Above'}
                onClick={() => setDirection('Above')}
              >
                {t('alerts.aboveLabel')}
              </Button>
              <Button
                variant="danger"
                outline={direction !== 'Below'}
                onClick={() => setDirection('Below')}
              >
                {t('alerts.belowLabel')}
              </Button>
            </ButtonGroup>
          </Form.Group>
          <Form.Group className="mb-3" controlId="alert-trigger">
            <Form.Label>{t('alerts.triggerPrice')}</Form.Label>
            <Form.Control
              type="number"
              step="any"
              min={0}
              value={trigger}
              onChange={(e) => setTrigger(e.target.value)}
              placeholder={quote ? formatCurrency(quote.price) : '—'}
              autoFocus
            />
          </Form.Group>
        </Modal.Body>
        <Modal.Footer>
          <Button variant="secondary" outline onClick={onClose} disabled={create.isPending}>{t('common.cancel')}</Button>
          <Button type="submit" variant="primary" disabled={!isValid} loading={create.isPending}>
            {t('alerts.setAlert')}
          </Button>
        </Modal.Footer>
      </Form>
    </Modal>
  )
}
