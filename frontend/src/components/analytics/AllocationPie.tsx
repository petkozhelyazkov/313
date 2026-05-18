import { ResponsiveContainer, PieChart, Pie, Cell, Tooltip, Legend } from 'recharts'
import { Card, Spinner } from 'react-bootstrap'
import { useTranslation } from 'react-i18next'
import { useAnalyticsAllocation } from '../../api/analytics'
import { formatCurrency, formatPercent } from '../../lib/format'

const PIE_COLORS = ['#0d6efd', '#198754', '#dc3545', '#fd7e14', '#6f42c1', '#20c997', '#ffc107', '#0dcaf0', '#d63384', '#6610f2']

export function AllocationPie() {
  const { t } = useTranslation()
  const { data, isLoading, isError } = useAnalyticsAllocation()

  return (
    <Card className="h-100">
      <Card.Header><strong>{t('analytics.allocation')}</strong></Card.Header>
      <Card.Body style={{ minHeight: 320 }}>
        {isLoading ? (
          <div className="d-flex justify-content-center align-items-center" style={{ minHeight: 280 }}>
            <Spinner animation="border" />
          </div>
        ) : isError ? (
          <div className="text-center text-muted py-5">{t('analytics.couldNotLoadAllocation')}</div>
        ) : !data || data.length === 0 ? (
          <div className="text-center text-muted py-5">{t('analytics.noPositions')}</div>
        ) : (
          <ResponsiveContainer width="100%" height={280}>
            <PieChart>
              <Pie
                data={data}
                dataKey="value"
                nameKey="symbol"
                outerRadius={100}
                isAnimationActive={false}
                label={(props) => `${(props as { symbol?: string }).symbol ?? ''}`}
              >
                {data.map((_, i) => (
                  <Cell key={i} fill={PIE_COLORS[i % PIE_COLORS.length]} />
                ))}
              </Pie>
              <Tooltip
                formatter={(value, name, item) => {
                  const slice = (item as unknown as { payload?: { weight?: number } })?.payload
                  const weight = slice?.weight
                  return [
                    `${formatCurrency(Number(value))} (${weight === undefined ? '' : formatPercent(weight)})`,
                    String(name),
                  ]
                }}
              />
              <Legend />
            </PieChart>
          </ResponsiveContainer>
        )}
      </Card.Body>
    </Card>
  )
}
