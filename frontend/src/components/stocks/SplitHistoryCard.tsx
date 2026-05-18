import { Card, Table, Spinner } from 'react-bootstrap'
import { useTranslation } from 'react-i18next'
import { Icon } from '../Icon'
import { useSplitHistory } from '../../api/splits'
import { TablePagination } from '../TablePagination'
import { usePagedData } from '../../hooks/usePagedData'

export function SplitHistoryCard({ symbol }: { symbol: string }) {
  const { t } = useTranslation()
  const { data, isLoading } = useSplitHistory(symbol)
  const paged = usePagedData(data ?? [], { defaultPageSize: 10, storageKey: 'splitHistory' })

  if (isLoading) {
    return (
      <Card className="mb-3">
        <Card.Header>
          <strong><Icon name="sr-target" className="me-2 text-info" /> {t('splits.history')}</strong>
        </Card.Header>
        <Card.Body className="text-center py-3">
          <Spinner animation="border" size="sm" />
        </Card.Body>
      </Card>
    )
  }

  if (!data || data.length === 0) return null

  return (
    <Card className="mb-3">
      <Card.Header className="d-flex justify-content-between align-items-center">
        <strong><Icon name="sr-target" className="me-2 text-info" /> {t('splits.history')}</strong>
        <span className="badge bg-secondary">{data.length}</span>
      </Card.Header>
      <Card.Body className="p-0">
        <Table responsive size="sm" className="mb-0 align-middle small">
          <thead>
            <tr>
              <th>{t('splits.date')}</th>
              <th className="text-end">{t('splits.ratio')}</th>
            </tr>
          </thead>
          <tbody>
            {paged.items.map((s, i) => (
              <tr key={`${s.date}-${i}`}>
                <td>{s.date}</td>
                <td className="text-end fw-semibold">{s.ratio}</td>
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
