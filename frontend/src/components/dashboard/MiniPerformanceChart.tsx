import { Card, Spinner } from 'react-bootstrap'
import { Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import {
  ResponsiveContainer,
  LineChart,
  Line,
  XAxis,
  YAxis,
  Tooltip,
} from 'recharts'
import { useAnalyticsSnapshots } from '../../api/analytics'
import { formatCurrency } from '../../lib/format'

export function MiniPerformanceChart() {
  const { t } = useTranslation()
  const { data, isLoading, isError } = useAnalyticsSnapshots('1M')

  return (
    <Card>
      <Card.Header className="d-flex justify-content-between align-items-center">
        <strong>{t('dashboard.miniPerformance')}</strong>
        <Link to="/analytics" className="small text-decoration-none">{t('dashboard.more')} →</Link>
      </Card.Header>
      <Card.Body style={{ minHeight: 220 }}>
        {isLoading ? (
          <div className="d-flex justify-content-center align-items-center" style={{ minHeight: 180 }}>
            <Spinner animation="border" size="sm" />
          </div>
        ) : isError ? (
          <div className="text-muted text-center py-4">{t('dashboard.miniPerformanceError')}</div>
        ) : !data || data.length < 2 ? (
          <div className="text-muted text-center py-4">{t('dashboard.miniPerformanceEmpty')}</div>
        ) : (
          <ResponsiveContainer width="100%" height={200}>
            <LineChart data={data} margin={{ top: 5, right: 12, bottom: 0, left: 0 }}>
              <XAxis dataKey="date" hide />
              <YAxis hide domain={['auto', 'auto']} />
              <Tooltip
                formatter={(value) => [formatCurrency(Number(value)), t('dashboard.total')]}
                labelFormatter={(label) => new Date(String(label)).toLocaleDateString()}
              />
              <Line
                type="monotone"
                dataKey="totalValue"
                stroke="#0d6efd"
                strokeWidth={2}
                dot={false}
                isAnimationActive={false}
              />
            </LineChart>
          </ResponsiveContainer>
        )}
      </Card.Body>
    </Card>
  )
}
