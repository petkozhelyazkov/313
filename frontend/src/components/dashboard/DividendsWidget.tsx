import { Card, Table } from 'react-bootstrap'
import { useTranslation, Trans } from 'react-i18next'
import { useUpcomingDividends, useDividendSummary } from '../../api/dividends'
import { formatCurrency } from '../../lib/format'
import { SymbolLogo } from '../SymbolLogo'
import { Icon } from '../Icon'
import { TablePagination } from '../TablePagination'
import { usePagedData } from '../../hooks/usePagedData'

export function DividendsWidget() {
  const { t } = useTranslation()
  const { data: upcoming, isLoading: upL } = useUpcomingDividends()
  const { data: summary, isLoading: sumL } = useDividendSummary()
  const paged = usePagedData(upcoming ?? [], { defaultPageSize: 10, storageKey: 'dividendsUpcoming' })

  if (upL || sumL) return null

  const hasUpcoming = upcoming && upcoming.length > 0
  const hasHistory = summary && summary.lifetimeReceived > 0

  if (!hasUpcoming && !hasHistory) return null

  return (
    <Card className="mb-3">
      <Card.Header className="d-flex justify-content-between align-items-center">
        <strong>
          <Icon name="sr-money-bill-wave" className="me-2 text-success" /> {t('dividends.title')}
        </strong>
        {summary && summary.uniqueSymbols > 0 && (
          <span className="text-muted small">
            {t('dividends.lifetime')}: <strong className="text-success">{formatCurrency(summary.lifetimeReceived)}</strong>{' '}
            <Trans i18nKey="dividends.fromStocks" values={{ count: summary.uniqueSymbols }} />
          </span>
        )}
      </Card.Header>
      <Card.Body className="p-0">
        {summary && (
          <div className="d-flex border-bottom px-3 py-2 small gap-3 flex-wrap">
            <div>
              <span className="text-muted me-1">{t('dividends.next30Days')}:</span>
              <strong>{formatCurrency(summary.upcoming30Days)}</strong>
            </div>
            <div>
              <span className="text-muted me-1">{t('dividends.last12Months')}:</span>
              <strong>{formatCurrency(summary.last12Months)}</strong>
            </div>
          </div>
        )}

        {hasUpcoming && (
          <>
            <Table size="sm" responsive className="mb-0 align-middle small">
              <thead>
                <tr>
                  <th>{t('portfolio.symbol')}</th>
                  <th>{t('dividends.exDate')}</th>
                  <th>{t('dividends.payDate')}</th>
                  <th className="text-end">{t('dividends.amountPerShare')}</th>
                  <th className="text-end">{t('dividends.shares')}</th>
                  <th className="text-end">{t('dividends.estimated')}</th>
                </tr>
              </thead>
              <tbody>
                {paged.items.map((d, i) => (
                  <tr key={`${d.symbol}-${d.exDate}-${i}`}>
                    <td>
                      <div className="d-flex align-items-center gap-2">
                        <SymbolLogo symbol={d.symbol} logoUrl={d.logoUrl} size={20} />
                        <strong>{d.symbol}</strong>
                      </div>
                    </td>
                    <td>{d.exDate}</td>
                    <td className="text-muted">{d.paymentDate ?? '—'}</td>
                    <td className="text-end">{formatCurrency(d.amountPerShare)}</td>
                    <td className="text-end">{d.currentQuantity.toLocaleString()}</td>
                    <td className="text-end fw-semibold text-success">
                      +{formatCurrency(d.estimatedPayment)}
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

        {!hasUpcoming && hasHistory && (
          <div className="p-3 text-muted small">{t('dividends.noUpcoming')}</div>
        )}
      </Card.Body>
    </Card>
  )
}
