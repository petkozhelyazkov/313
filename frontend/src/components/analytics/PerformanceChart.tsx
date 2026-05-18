import { useState } from 'react'
import {
  ResponsiveContainer,
  LineChart,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ReferenceLine,
  Legend,
} from 'recharts'
import { Card, Spinner, Form } from 'react-bootstrap'
import { useTranslation } from 'react-i18next'
import { Button, ButtonGroup } from '../Button'
import { useAnalyticsSnapshots, type AnalyticsRange } from '../../api/analytics'
import { formatCurrency } from '../../lib/format'

const RANGES: AnalyticsRange[] = ['1M', '3M', '6M', '1Y', 'MAX']

type Props = {
  range: AnalyticsRange
  onRangeChange: (r: AnalyticsRange) => void
}

export function PerformanceChart({ range, onRangeChange }: Props) {
  const { t } = useTranslation()
  const [compareSpy, setCompareSpy] = useState(false)
  const { data, isLoading, isError } = useAnalyticsSnapshots(range, compareSpy)
  const invested = data && data.length > 0 ? data[data.length - 1].totalInvested : 0

  const points = (data ?? []).map((p) => ({
    date: p.date,
    totalValue: p.totalValue,
    holdingsValue: p.holdingsValue,
    benchmark: p.benchmark,
  }))

  return (
    <Card className="mb-3">
      <Card.Header className="d-flex justify-content-between align-items-center flex-wrap gap-2">
        <strong>{t('analytics.performance')}</strong>
        <div className="d-flex align-items-center gap-3">
          <Form.Check
            type="switch"
            id="compare-spy"
            label={t('analytics.vsBenchmark')}
            checked={compareSpy}
            onChange={(e) => setCompareSpy(e.target.checked)}
          />
          <ButtonGroup size="sm">
            {RANGES.map((r) => (
              <Button
                key={r}
                size="sm"
                variant="primary"
                outline={r !== range}
                onClick={() => onRangeChange(r)}
              >
                {r}
              </Button>
            ))}
          </ButtonGroup>
        </div>
      </Card.Header>
      <Card.Body style={{ minHeight: 340 }}>
        {isLoading ? (
          <div className="d-flex justify-content-center align-items-center" style={{ minHeight: 300 }}>
            <Spinner animation="border" />
          </div>
        ) : isError ? (
          <div className="text-center text-muted py-5">{t('analytics.couldNotLoad')}</div>
        ) : points.length === 0 ? (
          <div className="text-center text-muted py-5">{t('analytics.noSnapshots')}</div>
        ) : (
          <ResponsiveContainer width="100%" height={320}>
            <LineChart data={points} margin={{ top: 5, right: 16, bottom: 0, left: 0 }}>
              <CartesianGrid strokeDasharray="3 3" stroke="#eef" />
              <XAxis
                dataKey="date"
                tick={{ fontSize: 12 }}
                tickFormatter={(d) => new Date(d).toLocaleDateString('en-US', { month: 'short', day: 'numeric' })}
                minTickGap={50}
              />
              <YAxis
                domain={['auto', 'auto']}
                tickFormatter={(v) => `$${Math.round(Number(v) / 1000)}k`}
                tick={{ fontSize: 12 }}
                width={64}
              />
              <Tooltip
                formatter={(value, name) => [formatCurrency(Number(value)), String(name)]}
                labelFormatter={(label) => new Date(String(label)).toLocaleDateString()}
              />
              <Legend />
              {invested > 0 && (
                <ReferenceLine
                  y={invested}
                  stroke="#6c757d"
                  strokeDasharray="3 3"
                  label={{ value: t('analytics.investedLine'), position: 'right', fill: '#6c757d', fontSize: 11 }}
                />
              )}
              <Line
                type="monotone"
                dataKey="totalValue"
                stroke="#0d6efd"
                strokeWidth={2}
                dot={false}
                isAnimationActive={false}
                name={t('analytics.totalValueLine')}
              />
              <Line
                type="monotone"
                dataKey="holdingsValue"
                stroke="#198754"
                strokeWidth={1.5}
                dot={false}
                isAnimationActive={false}
                name={t('analytics.holdingsOnlyLine')}
                strokeDasharray="4 4"
              />
              {compareSpy && (
                <Line
                  type="monotone"
                  dataKey="benchmark"
                  stroke="#fd7e14"
                  strokeWidth={2}
                  dot={false}
                  isAnimationActive={false}
                  name={t('analytics.benchmarkLine')}
                />
              )}
            </LineChart>
          </ResponsiveContainer>
        )}
      </Card.Body>
    </Card>
  )
}
