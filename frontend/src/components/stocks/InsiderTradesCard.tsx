import { Card, Table, Spinner } from 'react-bootstrap'
import { useTranslation } from 'react-i18next'
import { useInsiders } from '../../api/stocks'
import { formatCurrency, formatQty } from '../../lib/format'

type Props = { symbol: string }

export function InsiderTradesCard({ symbol }: Props) {
  const { t, i18n } = useTranslation()
  const { data, isLoading } = useInsiders(symbol)

  if (isLoading) {
    return (
      <Card className="mb-3">
        <Card.Body className="text-center py-4"><Spinner size="sm" /></Card.Body>
      </Card>
    )
  }
  if (!data || data.recentTrades.length === 0) return null

  return (
    <Card className="mb-3">
      <Card.Header><strong>{t('insider.title')}</strong></Card.Header>
      <Card.Body>
        <div className="d-flex gap-4 mb-3 flex-wrap">
          <div>
            <div className="text-muted text-uppercase small">{t('insider.last90Buys')}</div>
            <strong className="text-success fs-5">{data.last90DaysBuyCount}</strong>
            {data.last90DaysBuyValue > 0 && (
              <small className="text-muted ms-1">· {formatCurrency(data.last90DaysBuyValue)}</small>
            )}
          </div>
          <div>
            <div className="text-muted text-uppercase small">{t('insider.last90Sells')}</div>
            <strong className="text-danger fs-5">{data.last90DaysSellCount}</strong>
            {data.last90DaysSellValue > 0 && (
              <small className="text-muted ms-1">· {formatCurrency(data.last90DaysSellValue)}</small>
            )}
          </div>
        </div>
        <Table responsive size="sm" className="align-middle mb-0">
          <thead>
            <tr>
              <th>{t('insider.person')}</th>
              <th>{t('insider.type')}</th>
              <th className="text-end">{t('insider.shares')}</th>
              <th className="text-end">{t('insider.price')}</th>
              <th className="text-end">{t('insider.value')}</th>
              <th className="text-end">{t('insider.date')}</th>
            </tr>
          </thead>
          <tbody>
            {data.recentTrades.slice(0, 10).map((tr) => (
              <tr key={tr.id}>
                <td>
                  <div className="fw-semibold">{tr.personName}</div>
                  {tr.role && <small className="text-muted">{tr.role}</small>}
                </td>
                <td>
                  <span className={`badge ${tr.transactionType === 'Buy' ? 'bg-success' : tr.transactionType === 'Sell' ? 'bg-danger' : 'bg-secondary'}`}>
                    {t(`insider.txn.${tr.transactionType.toLowerCase()}`, { defaultValue: tr.transactionType })}
                  </span>
                </td>
                <td className="text-end">{formatQty(tr.shares)}</td>
                <td className="text-end">{tr.pricePerShare !== null ? formatCurrency(tr.pricePerShare) : '—'}</td>
                <td className="text-end">{tr.value !== null ? formatCurrency(tr.value) : '—'}</td>
                <td className="text-end small text-muted">
                  {new Date(tr.transactionDate).toLocaleDateString(i18n.language, { month: 'short', day: 'numeric', year: 'numeric' })}
                </td>
              </tr>
            ))}
          </tbody>
        </Table>
      </Card.Body>
    </Card>
  )
}
