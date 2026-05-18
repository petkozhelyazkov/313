import { ResponsiveContainer, BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, Cell, ReferenceLine } from 'recharts'
import { Card, Spinner } from 'react-bootstrap'
import { useTranslation } from 'react-i18next'
import { useAnalyticsReturns } from '../../api/analytics'
import { formatCurrency } from '../../lib/format'

export function ReturnsBarChart() {
  const { t } = useTranslation()
  const { data, isLoading, isError } = useAnalyticsReturns()

  return (
    <Card className="h-100">
      <Card.Header><strong>{t('analytics.returns')}</strong></Card.Header>
      <Card.Body style={{ minHeight: 320 }}>
        {isLoading ? (
          <div className="d-flex justify-content-center align-items-center" style={{ minHeight: 280 }}>
            <Spinner animation="border" />
          </div>
        ) : isError ? (
          <div className="text-center text-muted py-5">{t('analytics.couldNotLoadReturns')}</div>
        ) : !data || data.length === 0 ? (
          <div className="text-center text-muted py-5">{t('analytics.noPositions')}</div>
        ) : (
          <ResponsiveContainer width="100%" height={280}>
            <BarChart data={data} margin={{ top: 10, right: 16, bottom: 0, left: 0 }}>
              <CartesianGrid strokeDasharray="3 3" stroke="#eef" />
              <XAxis dataKey="symbol" tick={{ fontSize: 12 }} />
              <YAxis
                tick={{ fontSize: 12 }}
                tickFormatter={(v) => formatCurrency(Number(v))}
                width={80}
              />
              <Tooltip
                formatter={(value, name) => [formatCurrency(Number(value)), String(name)]}
              />
              <ReferenceLine y={0} stroke="#000" />
              <Bar dataKey="totalPl" name={t('analytics.totalPl')} isAnimationActive={false}>
                {data.map((row, i) => (
                  <Cell key={i} fill={row.totalPl >= 0 ? '#198754' : '#dc3545'} />
                ))}
              </Bar>
            </BarChart>
          </ResponsiveContainer>
        )}
      </Card.Body>
    </Card>
  )
}
