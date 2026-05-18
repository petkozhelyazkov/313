import { Card, Table, Spinner } from 'react-bootstrap'
import { useTranslation } from 'react-i18next'
import { useDividendHistory } from '../../api/dividends'
import { formatCurrency } from '../../lib/format'
import { Icon } from '../Icon'
import { TablePagination } from '../TablePagination'
import { usePagedData } from '../../hooks/usePagedData'

export function DividendHistoryCard({ symbol }: { symbol: string }) {
  const { t } = useTranslation()
  const { data, isLoading } = useDividendHistory(symbol)
  const paged = usePagedData(data ?? [], { defaultPageSize: 10, storageKey: 'dividendHistory' })

  if (isLoading) {
    return (
      <Card className="mb-3">
        <Card.Header><strong><Icon name="sr-money-bill-wave" className="me-2 text-success" /> {t('dividends.history')}</strong></Card.Header>
        <Card.Body className="text-center py-3">
          <Spinner animation="border" size="sm" />
        </Card.Body>
      </Card>
    )
  }

  if (!data || data.length === 0) return null

  const lastYear = data.filter((d) => {
    const ex = new Date(d.exDate)
    const oneYearAgo = new Date()
    oneYearAgo.setFullYear(oneYearAgo.getFullYear() - 1)
    return ex >= oneYearAgo && ex <= new Date()
  })
  const ttm = lastYear.reduce((s, d) => s + d.amountPerShare, 0)

  return (
    <Card className="mb-3">
      <Card.Header className="d-flex justify-content-between align-items-center">
        <strong><Icon name="sr-money-bill-wave" className="me-2 text-success" /> {t('dividends.history')}</strong>
        {ttm > 0 && (
          <span className="text-muted small">
            {t('dividends.ttm')}: <strong className="text-success">{formatCurrency(ttm)}{t('dividends.perShare')}</strong>
          </span>
        )}
      </Card.Header>
      <Card.Body className="p-0">
        <Table responsive size="sm" className="mb-0 align-middle small">
          <thead>
            <tr>
              <th>{t('dividends.exDate')}</th>
              <th>{t('dividends.payDate')}</th>
              <th className="text-end">{t('dividends.amountPerShare')}</th>
            </tr>
          </thead>
          <tbody>
            {paged.items.map((d, i) => (
              <tr key={`${d.exDate}-${i}`}>
                <td>{d.exDate}</td>
                <td className="text-muted">{d.paymentDate ?? '—'}</td>
                <td className="text-end fw-semibold">{formatCurrency(d.amountPerShare)}</td>
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
      </Card.Body>
    </Card>
  )
}
