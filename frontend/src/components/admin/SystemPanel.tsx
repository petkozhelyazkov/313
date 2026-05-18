import { Card, Row, Col, ProgressBar, Spinner, Table } from 'react-bootstrap'
import { useTranslation } from 'react-i18next'
import { Button } from '../Button'
import { TablePagination } from '../TablePagination'
import { usePagedData } from '../../hooks/usePagedData'
import { useApiUsage, useRunSnapshots } from '../../api/admin'
import { toast } from '../../lib/toast'
import { getApiErrorMessage } from '../../api/client'

export function SystemPanel() {
  const { t } = useTranslation()
  const { data, isLoading, isError } = useApiUsage()
  const runSnapshots = useRunSnapshots()
  const paged = usePagedData(data?.recentCalls ?? [], { defaultPageSize: 10, storageKey: 'apiUsage' })

  const onRun = async () => {
    try {
      const res = await runSnapshots.mutateAsync()
      toast.success(t('admin.snapshotsRun', { date: res.date, users: res.processedUsers }))
    } catch (err) {
      toast.error(getApiErrorMessage(err))
    }
  }

  return (
    <Card className="mb-3">
      <Card.Header className="d-flex justify-content-between align-items-center">
        <strong>{t('admin.system')}</strong>
        <Button size="sm" variant="primary" onClick={onRun} loading={runSnapshots.isPending}>
          {t('admin.runSnapshots')}
        </Button>
      </Card.Header>
      <Card.Body>
        {isLoading ? (
          <div className="text-center"><Spinner size="sm" animation="border" /></div>
        ) : isError || !data ? (
          <div className="text-muted">{t('admin.couldNotLoadUsage')}</div>
        ) : (
          <Row className="g-3">
            <Col md={6}>
              <div className="text-muted small text-uppercase">{t('admin.twelveDataToday')}</div>
              <strong>
                {data.today.count} / {data.today.quota}
              </strong>
              <ProgressBar
                now={Math.min(100, data.today.percentUsed)}
                variant={data.today.percentUsed > 80 ? 'warning' : 'info'}
                className="mt-1"
                style={{ height: 8 }}
              />
              <small className="text-muted">{t('admin.percentUsed', { pct: data.today.percentUsed.toFixed(1) })}</small>
            </Col>
            <Col md={6}>
              <div className="text-muted small text-uppercase">{t('admin.lastHour')}</div>
              <strong>
                {data.lastHour.count} / {data.lastHour.quota}
              </strong>
              <ProgressBar
                now={Math.min(100, data.lastHour.percentUsed)}
                variant={data.lastHour.percentUsed > 80 ? 'warning' : 'info'}
                className="mt-1"
                style={{ height: 8 }}
              />
              <small className="text-muted">{t('admin.percentUsed', { pct: data.lastHour.percentUsed.toFixed(1) })}</small>
            </Col>
            <Col xs={12}>
              <div className="text-muted small text-uppercase mb-2">{t('admin.recentCalls')}</div>
              <Table size="sm" responsive className="mb-0">
                <thead>
                  <tr>
                    <th>{t('admin.when')}</th>
                    <th>{t('admin.endpoint')}</th>
                    <th>{t('admin.symbols')}</th>
                    <th>{t('admin.status')}</th>
                    <th className="text-end">{t('admin.ms')}</th>
                  </tr>
                </thead>
                <tbody>
                  {paged.items.map((c) => (
                    <tr key={c.id}>
                      <td className="small">{new Date(c.requestedAt).toLocaleTimeString()}</td>
                      <td className="small">{c.endpoint}</td>
                      <td className="small">{c.symbols ?? '—'}</td>
                      <td>
                        <span className={c.statusCode >= 200 && c.statusCode < 300 ? 'text-success' : 'text-danger'}>
                          {c.statusCode}
                        </span>
                      </td>
                      <td className="text-end small">{c.responseTimeMs}</td>
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
            </Col>
          </Row>
        )}
      </Card.Body>
    </Card>
  )
}
