import { Card, Table, Badge, Spinner } from 'react-bootstrap'
import { useTranslation } from 'react-i18next'
import { Button } from './Button'
import { Link } from 'react-router-dom'
import { useAlerts, useCancelAlert, useAcknowledgeAlert } from '../api/alerts'
import { SymbolLogo } from './SymbolLogo'
import { TablePagination } from './TablePagination'
import { usePagedData } from '../hooks/usePagedData'
import { formatCurrency } from '../lib/format'
import { toast } from '../lib/toast'
import { getApiErrorMessage } from '../api/client'

const statusVariant: Record<string, string> = {
  Active: 'warning',
  Triggered: 'success',
  Cancelled: 'secondary',
}

const statusKey: Record<string, string> = {
  Active: 'alerts.statusActive',
  Triggered: 'alerts.statusTriggered',
  Cancelled: 'alerts.statusCancelled',
}

export function AlertsList() {
  const { t } = useTranslation()
  const { data, isLoading } = useAlerts()
  const cancel = useCancelAlert()
  const ack = useAcknowledgeAlert()
  const paged = usePagedData(data ?? [], { defaultPageSize: 10, storageKey: 'alerts' })

  const handleCancel = async (id: number) => {
    try {
      await cancel.mutateAsync(id)
      toast.success(t('toast.alertCancelled'))
    } catch (err) {
      toast.error(getApiErrorMessage(err))
    }
  }

  const handleAck = async (id: number) => {
    try {
      await ack.mutateAsync(id)
    } catch (err) {
      toast.error(getApiErrorMessage(err))
    }
  }

  return (
    <Card id="alerts">
      <Card.Header><strong>{t('alerts.title')}</strong></Card.Header>
      <Card.Body className="p-0">
        {isLoading ? (
          <div className="text-center py-4"><Spinner size="sm" animation="border" /></div>
        ) : !data || data.length === 0 ? (
          <div className="text-muted text-center py-4 small">{t('alerts.empty')}</div>
        ) : (
          <>
            <Table responsive className="mb-0 align-middle">
              <thead>
                <tr>
                  <th>{t('portfolio.symbol')}</th>
                  <th>{t('alerts.direction')}</th>
                  <th className="text-end">{t('alerts.trigger')}</th>
                  <th className="text-end">{t('orders.current')}</th>
                  <th>{t('orders.status')}</th>
                  <th>{t('orders.created')}</th>
                  <th />
                </tr>
              </thead>
              <tbody>
                {paged.items.map((a) => (
                  <tr key={a.id} className={a.status === 'Triggered' && !a.acknowledged ? 'table-success' : ''}>
                    <td>
                      <Link to={`/stocks/${encodeURIComponent(a.symbol)}`} className="text-decoration-none d-inline-flex align-items-center gap-2">
                        <SymbolLogo symbol={a.symbol} logoUrl={a.logoUrl} size={22} />
                        <strong>{a.symbol}</strong>
                      </Link>
                    </td>
                    <td>{a.direction === 'Above' ? t('alerts.aboveLabel') : t('alerts.belowLabel')}</td>
                    <td className="text-end">{formatCurrency(a.triggerPrice)}</td>
                    <td className="text-end">{a.currentPrice != null ? formatCurrency(a.currentPrice) : '—'}</td>
                    <td>
                      <Badge bg={statusVariant[a.status] ?? 'secondary'}>
                        {t(statusKey[a.status] ?? a.status, { defaultValue: a.status })}
                      </Badge>
                      {a.triggeredPrice != null && (
                        <div><small className="text-muted">@ {formatCurrency(a.triggeredPrice)}</small></div>
                      )}
                    </td>
                    <td className="small text-muted">{new Date(a.createdAt).toLocaleDateString()}</td>
                    <td className="text-end">
                      {a.status === 'Active' && (
                        <Button size="sm" variant="secondary" outline onClick={() => handleCancel(a.id)}>
                          {t('common.cancel')}
                        </Button>
                      )}
                      {a.status === 'Triggered' && !a.acknowledged && (
                        <Button size="sm" variant="success" outline onClick={() => handleAck(a.id)}>
                          {t('alerts.gotIt')}
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
          </>
        )}
      </Card.Body>
    </Card>
  )
}
