import { ResponsiveContainer, PieChart, Pie, Cell, Tooltip, Legend } from 'recharts'
import { Card, Spinner } from 'react-bootstrap'
import { useTranslation } from 'react-i18next'
import { useAnalyticsSectorAllocation } from '../../api/analytics'
import { formatCurrency, formatPercent } from '../../lib/format'

const PIE_COLORS = ['#0d6efd', '#198754', '#dc3545', '#fd7e14', '#6f42c1', '#20c997', '#0dcaf0', '#d63384', '#6610f2', '#0a58ca']

export function SectorPie() {
  const { t } = useTranslation()
  const { data, isLoading, isError } = useAnalyticsSectorAllocation()

  return (
    <Card className="h-100">
      <Card.Header><strong>{t('analytics.sectorAllocation')}</strong></Card.Header>
      <Card.Body style={{ minHeight: 320 }}>
        {isLoading ? (
          <div className="d-flex justify-content-center align-items-center" style={{ minHeight: 280 }}>
            <Spinner animation="border" />
          </div>
        ) : isError ? (
          <div className="text-center text-muted py-5">{t('analytics.couldNotLoadSector')}</div>
        ) : !data || data.length === 0 ? (
          <div className="text-center text-muted py-5">{t('analytics.noPositions')}</div>
        ) : (
          <ResponsiveContainer width="100%" height={280}>
            <PieChart>
              <Pie
                data={data}
                dataKey="value"
                nameKey="sector"
                outerRadius={100}
                isAnimationActive={false}
                label={(props) => (props as { sector?: string }).sector ?? ''}
              >
                {data.map((_slice, i) => (
                  <Cell key={i} fill={PIE_COLORS[i % PIE_COLORS.length]} />
                ))}
              </Pie>
              <Tooltip
                formatter={(value, name, item) => {
                  const slice = (item as unknown as { payload?: { weight?: number; symbols?: number } })?.payload
                  const count = slice?.symbols ?? 0
                  const symLabel = t('analytics.symbolsInSector', { count })
                  return [
                    `${formatCurrency(Number(value))} (${slice?.weight == null ? '' : formatPercent(slice.weight)}, ${symLabel})`,
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
