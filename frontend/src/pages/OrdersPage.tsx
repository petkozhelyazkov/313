import { Container, Tab, Tabs, Table, Badge, Card, Spinner } from 'react-bootstrap'
import { useTranslation } from 'react-i18next'
import { Button } from '../components/Button'
import { Link } from 'react-router-dom'
import { useOrders, useCancelOrder, type PendingOrderDto } from '../api/orders'
import { SymbolLogo } from '../components/SymbolLogo'
import { TablePagination } from '../components/TablePagination'
import { usePagedData } from '../hooks/usePagedData'
import { useTabFromUrl } from '../hooks/useTabFromUrl'
import { useDocumentTitle } from '../hooks/useDocumentTitle'
import { formatCurrency, formatQty } from '../lib/format'
import { toast } from '../lib/toast'
import { getApiErrorMessage } from '../api/client'

const statusVariant: Record<string, string> = {
  Pending: 'warning',
  Filled: 'success',
  Cancelled: 'secondary',
  Expired: 'secondary',
  FailedExecution: 'danger',
}

const sideLabelKey: Record<string, string> = {
  LimitBuy: 'orders.limitBuy',
  LimitSell: 'orders.limitSell',
  StopLoss: 'orders.stopLoss',
  TrailingStop: 'trade2.trailingStop',
}

const statusLabelKey: Record<string, string> = {
  Pending: 'orders.statusPending',
  Filled: 'orders.statusFilled',
  Cancelled: 'orders.statusCancelled',
  Expired: 'orders.statusExpired',
  FailedExecution: 'orders.statusFailedExecution',
}

export function OrdersPage() {
  const { t } = useTranslation()
  useDocumentTitle(t('orders.title'))
  const { data, isLoading } = useOrders()
  const cancel = useCancelOrder()
  const [activeTab, setActiveTab] = useTabFromUrl('tab', 'open')

  const handleCancel = async (id: number, symbol: string) => {
    try {
      await cancel.mutateAsync(id)
      toast.success(t('toast.orderCancelled', { symbol }))
    } catch (err) {
      toast.error(getApiErrorMessage(err))
    }
  }

  return (
    <Container className="py-4">
      <h1 className="h3 mb-3">{t('orders.title')}</h1>

      {isLoading || !data ? (
        <Card body className="text-center">
          <Spinner animation="border" size="sm" /> {t('common.loadingDot')}…
        </Card>
      ) : (
        <Tabs activeKey={activeTab} onSelect={(k) => k && setActiveTab(k)}>
          <Tab eventKey="open" title={`${t('orders.open')} (${data.open.length})`}>
            <OrderTable
              orders={data.open}
              showCancel
              onCancel={handleCancel}
              isCancelling={cancel.isPending}
              storageKey="ordersOpen"
            />
          </Tab>
          <Tab eventKey="history" title={`${t('orders.history')} (${data.history.length})`}>
            <OrderTable orders={data.history} showCancel={false} storageKey="ordersHistory" />
          </Tab>
        </Tabs>
      )}
    </Container>
  )
}

function OrderTable({
  orders,
  showCancel,
  onCancel,
  isCancelling,
  storageKey,
}: {
  orders: PendingOrderDto[]
  showCancel: boolean
  onCancel?: (id: number, symbol: string) => void
  isCancelling?: boolean
  storageKey: string
}) {
  const { t } = useTranslation()
  const paged = usePagedData(orders, { defaultPageSize: 10, storageKey })
  if (orders.length === 0) {
    return (
      <Card body className="text-center text-muted py-5 mt-3">
        {showCancel ? t('orders.noOpen') : t('orders.noHistory')}
      </Card>
    )
  }
  return (
    <Card className="mt-3">
    <Table responsive hover className="align-middle mb-0">
      <thead>
        <tr>
          <th>{t('portfolio.symbol')}</th>
          <th>{t('orders.side')}</th>
          <th className="text-end">{t('common.quantity')}</th>
          <th className="text-end">{t('orders.limit')}</th>
          <th className="text-end">{t('orders.current')}</th>
          <th>{t('orders.status')}</th>
          <th>{t('orders.created')}</th>
          <th />
        </tr>
      </thead>
      <tbody>
        {paged.items.map((o) => (
          <tr key={o.id}>
            <td>
              <Link
                to={`/stocks/${encodeURIComponent(o.symbol)}`}
                className="text-decoration-none d-inline-flex align-items-center gap-2"
              >
                <SymbolLogo symbol={o.symbol} logoUrl={o.logoUrl} size={24} />
                <strong>{o.symbol}</strong>
              </Link>
            </td>
            <td>
              <span className={`badge bg-${o.side === 'LimitBuy' ? 'success' : 'danger'}`}>
                {t(sideLabelKey[o.side] ?? o.side, { defaultValue: o.side })}
              </span>
            </td>
            <td className="text-end">{formatQty(o.quantity)}</td>
            <td className="text-end">
              {formatCurrency(o.limitPrice)}
              {o.side === 'TrailingStop' && o.trailingStopPercent != null && (
                <div><small className="text-muted">−{o.trailingStopPercent}% trail</small></div>
              )}
              {o.side === 'TrailingStop' && o.highWaterMark != null && (
                <div><small className="text-muted">peak {formatCurrency(o.highWaterMark)}</small></div>
              )}
            </td>
            <td className="text-end">{o.currentPrice != null ? formatCurrency(o.currentPrice) : '—'}</td>
            <td>
              <Badge bg={statusVariant[o.status] ?? 'secondary'}>
                {t(statusLabelKey[o.status] ?? o.status, { defaultValue: o.status })}
              </Badge>
              {o.failureReason && <div><small className="text-danger">{o.failureReason}</small></div>}
              {o.filledPrice != null && (
                <div><small className="text-muted">{t('orders.filledAt', { price: formatCurrency(o.filledPrice) })}</small></div>
              )}
            </td>
            <td className="small text-muted">{new Date(o.createdAt).toLocaleString()}</td>
            <td className="text-end">
              {showCancel && (
                <Button
                  size="sm"
                  variant="secondary"
                  outline
                  onClick={() => onCancel?.(o.id, o.symbol)}
                  disabled={isCancelling}
                >
                  {t('common.cancel')}
                </Button>
              )}
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
    </Card>
  )
}
