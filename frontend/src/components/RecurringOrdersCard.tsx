import { useState } from 'react'
import { Card, Table, Modal, Form, Badge, Spinner } from 'react-bootstrap'
import { useTranslation, Trans } from 'react-i18next'
import { Button } from './Button'
import {
  useRecurringOrders,
  useCreateRecurringOrder,
  useUpdateRecurringOrder,
  useDeleteRecurringOrder,
  type RecurringFrequency,
} from '../api/recurringOrders'
import { SymbolSearchInput } from './watchlist/SymbolSearchInput'
import { formatCurrency } from '../lib/format'
import { toast } from '../lib/toast'
import { getApiErrorMessage } from '../api/client'
import { Icon } from './Icon'
import { useConfirm } from './ConfirmDialog'
import { TablePagination } from './TablePagination'
import { usePagedData } from '../hooks/usePagedData'

const FREQUENCIES: RecurringFrequency[] = ['Daily', 'Weekly', 'Biweekly', 'Monthly']

export function RecurringOrdersCard() {
  const { t } = useTranslation()
  const { data, isLoading } = useRecurringOrders()
  const create = useCreateRecurringOrder()
  const update = useUpdateRecurringOrder()
  const del = useDeleteRecurringOrder()
  const confirm = useConfirm()
  const [open, setOpen] = useState(false)
  const paged = usePagedData(data ?? [], { defaultPageSize: 10, storageKey: 'recurring' })

  const handleDelete = async (id: number, symbol: string) => {
    const ok = await confirm({
      title: t('recurring.deleteTitle'),
      body: <Trans i18nKey="recurring.deleteBody" values={{ symbol }} />,
      confirmLabel: t('common.delete'),
      cancelLabel: t('common.cancel'),
      variant: 'danger',
    })
    if (!ok) return
    try {
      await del.mutateAsync(id)
      toast.success(t('toast.recurringDeleted'))
    } catch (err) {
      toast.error(getApiErrorMessage(err))
    }
  }

  const handleToggle = async (id: number, isActive: boolean) => {
    try {
      await update.mutateAsync({ id, isActive: !isActive })
      toast.success(isActive ? t('toast.recurringPaused') : t('toast.recurringResumed'))
    } catch (err) {
      toast.error(getApiErrorMessage(err))
    }
  }

  return (
    <Card>
      <Card.Header className="d-flex justify-content-between align-items-center">
        <strong><Icon name="sr-refresh" className="me-2" /> {t('recurring.title')}</strong>
        <Button size="sm" variant="primary" onClick={() => setOpen(true)}>{t('recurring.newRule')}</Button>
      </Card.Header>
      <Card.Body className="p-0">
        {isLoading && (
          <div className="text-center py-3">
            <Spinner animation="border" size="sm" />
          </div>
        )}
        {!isLoading && (!data || data.length === 0) && (
          <div className="p-3 text-muted small">{t('recurring.empty')}</div>
        )}
        {data && data.length > 0 && (
          <>
          <Table responsive size="sm" className="mb-0 align-middle small">
            <thead>
              <tr>
                <th>{t('portfolio.symbol')}</th>
                <th>{t('recurring.amount')}</th>
                <th>{t('recurring.frequency')}</th>
                <th>{t('recurring.nextRun')}</th>
                <th>{t('recurring.lastRun')}</th>
                <th className="text-end">{t('recurring.runs')}</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {paged.items.map((r) => (
                <tr key={r.id} className={r.isActive ? '' : 'opacity-50'}>
                  <td className="fw-semibold">{r.symbol}</td>
                  <td>{formatCurrency(r.cashAmount)}</td>
                  <td>
                    <Badge bg="secondary">{t(`recurring.freq.${r.frequency}`, { defaultValue: r.frequency })}</Badge>
                  </td>
                  <td className="text-muted">{new Date(r.nextRunAt).toLocaleString()}</td>
                  <td className="text-muted">
                    {r.lastRunAt ? new Date(r.lastRunAt).toLocaleString() : '—'}
                    {r.lastFailureReason && (
                      <div className="text-danger" style={{ fontSize: '0.7rem' }}>{r.lastFailureReason}</div>
                    )}
                  </td>
                  <td className="text-end">
                    <span className="text-success">{r.successfulRuns}</span>
                    {r.failedRuns > 0 && (
                      <span className="text-danger"> / {r.failedRuns} <Icon name="rr-cross" /></span>
                    )}
                  </td>
                  <td className="text-end">
                    <Button
                      size="sm"
                      variant={r.isActive ? 'warning' : 'success'}
                      outline
                      className="me-1"
                      onClick={() => handleToggle(r.id, r.isActive)}
                      disabled={update.isPending}
                    >
                      {t(r.isActive ? 'recurring.pause' : 'recurring.resume')}
                    </Button>
                    <Button
                      size="sm"
                      variant="danger"
                      outline
                      onClick={() => handleDelete(r.id, r.symbol)}
                      disabled={del.isPending}
                    >
                      ×
                    </Button>
                  </td>
                </tr>
              ))}
            </tbody>
          </Table>
          <TablePagination
            page={paged.page}
            pageSize={paged.pageSize}
            totalCount={paged.total}
            onPageChange={paged.setPage}
            onPageSizeChange={paged.setPageSize}
          />
          </>
        )}
      </Card.Body>

      <CreateRuleModal
        show={open}
        onClose={() => setOpen(false)}
        onCreate={async (input) => {
          try {
            await create.mutateAsync(input)
            toast.success(
              t('toast.recurringCreated', {
                frequency: t(`recurring.freq.${input.frequency}`, { defaultValue: input.frequency }).toLowerCase(),
                symbol: input.symbol,
              }),
            )
            setOpen(false)
          } catch (err) {
            toast.error(getApiErrorMessage(err))
          }
        }}
        isPending={create.isPending}
      />
    </Card>
  )
}

function CreateRuleModal({
  show, onClose, onCreate, isPending,
}: {
  show: boolean
  onClose: () => void
  onCreate: (input: { symbol: string; cashAmount: number; frequency: RecurringFrequency }) => void
  isPending: boolean
}) {
  const { t } = useTranslation()
  const [symbol, setSymbol] = useState('')
  const [amount, setAmount] = useState('100')
  const [frequency, setFrequency] = useState<RecurringFrequency>('Weekly')

  const amtNum = Number(amount)
  const valid = symbol.length > 0 && Number.isFinite(amtNum) && amtNum > 0

  return (
    <Modal show={show} onHide={onClose} centered onShow={() => {
      setSymbol('')
      setAmount('100')
      setFrequency('Weekly')
    }}>
      <Modal.Header closeButton>
        <Modal.Title>{t('recurring.newTitle')}</Modal.Title>
      </Modal.Header>
      <Form onSubmit={(e) => {
        e.preventDefault()
        if (valid) onCreate({ symbol: symbol.toUpperCase(), cashAmount: amtNum, frequency })
      }}>
        <Modal.Body>
          <Form.Group className="mb-3">
            <Form.Label>{t('portfolio.symbol')}</Form.Label>
            <SymbolSearchInput onSelect={(s) => setSymbol(s)} />
            {symbol && (
              <div className="small text-muted mt-1">
                <Trans i18nKey="recurring.selected" values={{ symbol }} />
              </div>
            )}
          </Form.Group>
          <Form.Group className="mb-3">
            <Form.Label>{t('recurring.cashAmountLabel')}</Form.Label>
            <Form.Control
              type="number"
              step="any"
              min={1}
              value={amount}
              onChange={(e) => setAmount(e.target.value)}
            />
            <Form.Text className="text-muted">
              {t('recurring.cashAmountHint', { amount: amtNum.toFixed(2) })}
            </Form.Text>
          </Form.Group>
          <Form.Group className="mb-3">
            <Form.Label>{t('recurring.frequencyLabel')}</Form.Label>
            <Form.Select value={frequency} onChange={(e) => setFrequency(e.target.value as RecurringFrequency)}>
              {FREQUENCIES.map((f) => (
                <option key={f} value={f}>{t(`recurring.freq.${f}`, { defaultValue: f })}</option>
              ))}
            </Form.Select>
          </Form.Group>
        </Modal.Body>
        <Modal.Footer>
          <Button variant="secondary" outline onClick={onClose} disabled={isPending}>{t('common.cancel')}</Button>
          <Button type="submit" variant="primary" disabled={!valid} loading={isPending}>
            {t('recurring.createRule')}
          </Button>
        </Modal.Footer>
      </Form>
    </Modal>
  )
}
