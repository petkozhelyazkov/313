import { useMemo, useState } from 'react'
import { Card, Spinner, Form } from 'react-bootstrap'
import { useTranslation } from 'react-i18next'
import { Button, ButtonGroup } from '../Button'
import {
  ResponsiveContainer,
  LineChart,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ReferenceLine,
} from 'recharts'
import { useHistory, type Range } from '../../api/stocks'
import { formatCurrency } from '../../lib/format'
import { sma, ema, rsi, bollingerBands } from '../../lib/indicators'

const RANGES: Range[] = ['1M', '3M', '6M', '1Y', '5Y', 'MAX']

type Props = {
  symbol: string
  range: Range
  onRangeChange: (r: Range) => void
  /** Optional reference line at the user's average cost. */
  averageCost?: number | null
}

type Indicators = {
  sma20: boolean
  ema50: boolean
  bb: boolean
  rsi: boolean
}

export function PriceChart({ symbol, range, onRangeChange, averageCost }: Props) {
  const { t } = useTranslation()
  const { data, isLoading, isError } = useHistory(symbol, range)
  const [ind, setInd] = useState<Indicators>({ sma20: false, ema50: false, bb: false, rsi: false })

  const closes = useMemo(
    () => (data?.points ?? []).map((p) => ({ date: p.date, close: p.close })),
    [data],
  )

  const chartData = useMemo(() => {
    const smaArr = ind.sma20 ? sma(closes, 20) : null
    const emaArr = ind.ema50 ? ema(closes, 50) : null
    const bbArr = ind.bb ? bollingerBands(closes, 20, 2) : null
    return closes.map((p, i) => ({
      date: p.date,
      close: p.close,
      sma20: smaArr?.[i]?.value ?? null,
      ema50: emaArr?.[i]?.value ?? null,
      bbUpper: bbArr?.[i]?.upper ?? null,
      bbLower: bbArr?.[i]?.lower ?? null,
    }))
  }, [closes, ind])

  const rsiData = useMemo(() => (ind.rsi ? rsi(closes, 14) : null), [closes, ind.rsi])

  const ticks = chartData.length > 0
    ? [chartData[0].date, chartData[Math.floor(chartData.length / 2)].date, chartData[chartData.length - 1].date]
    : []

  return (
    <Card className="mb-3">
      <Card.Header className="d-flex justify-content-between align-items-center flex-wrap gap-2">
        <strong>{t('stock.priceRange', { range })}</strong>
        <div className="d-flex align-items-center gap-3 flex-wrap">
          <div className="d-flex gap-3 small">
            <Form.Check
              type="switch"
              id="ind-sma20"
              label="SMA 20"
              checked={ind.sma20}
              onChange={(e) => setInd({ ...ind, sma20: e.target.checked })}
            />
            <Form.Check
              type="switch"
              id="ind-ema50"
              label="EMA 50"
              checked={ind.ema50}
              onChange={(e) => setInd({ ...ind, ema50: e.target.checked })}
            />
            <Form.Check
              type="switch"
              id="ind-bb"
              label="Bollinger"
              checked={ind.bb}
              onChange={(e) => setInd({ ...ind, bb: e.target.checked })}
            />
            <Form.Check
              type="switch"
              id="ind-rsi"
              label="RSI"
              checked={ind.rsi}
              onChange={(e) => setInd({ ...ind, rsi: e.target.checked })}
            />
          </div>
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
      <Card.Body style={{ minHeight: 320 }}>
        {isLoading ? (
          <div className="d-flex justify-content-center align-items-center" style={{ minHeight: 300 }}>
            <Spinner animation="border" />
          </div>
        ) : isError ? (
          <div className="text-center text-muted py-5">Could not load price history.</div>
        ) : chartData.length === 0 ? (
          <div className="text-center text-muted py-5">No price data for this range.</div>
        ) : (
          <>
            <ResponsiveContainer width="100%" height={300}>
              <LineChart data={chartData} margin={{ top: 5, right: 16, bottom: 0, left: 0 }}>
                <CartesianGrid strokeDasharray="3 3" stroke="#eef" />
                <XAxis
                  dataKey="date"
                  ticks={ticks}
                  tickFormatter={(d) => new Date(d).toLocaleDateString('en-US', { month: 'short', year: '2-digit' })}
                  tick={{ fontSize: 12 }}
                />
                <YAxis
                  domain={['auto', 'auto']}
                  tickFormatter={(v) => `$${Number(v).toFixed(0)}`}
                  tick={{ fontSize: 12 }}
                  width={60}
                />
                <Tooltip
                  formatter={(value) => [formatCurrency(Number(value)), 'Close']}
                  labelFormatter={(label) => new Date(String(label)).toLocaleDateString()}
                />
                <Line
                  type="monotone"
                  dataKey="close"
                  stroke="#0d6efd"
                  strokeWidth={2}
                  dot={false}
                  isAnimationActive={false}
                  name="Close"
                />
                {ind.sma20 && (
                  <Line type="monotone" dataKey="sma20" stroke="#fd7e14" strokeWidth={1.5} dot={false} isAnimationActive={false} name="SMA 20" />
                )}
                {ind.ema50 && (
                  <Line type="monotone" dataKey="ema50" stroke="#6f42c1" strokeWidth={1.5} dot={false} isAnimationActive={false} name="EMA 50" />
                )}
                {ind.bb && (
                  <>
                    <Line type="monotone" dataKey="bbUpper" stroke="#dc3545" strokeWidth={1} dot={false} strokeDasharray="3 3" isAnimationActive={false} name="BB upper" />
                    <Line type="monotone" dataKey="bbLower" stroke="#198754" strokeWidth={1} dot={false} strokeDasharray="3 3" isAnimationActive={false} name="BB lower" />
                  </>
                )}
                {averageCost ? (
                  <ReferenceLine
                    y={averageCost}
                    stroke="#198754"
                    strokeDasharray="4 4"
                    label={{ value: `Avg cost ${formatCurrency(averageCost)}`, position: 'right', fill: '#198754', fontSize: 11 }}
                  />
                ) : null}
              </LineChart>
            </ResponsiveContainer>

            {ind.rsi && rsiData && (
              <ResponsiveContainer width="100%" height={120}>
                <LineChart data={rsiData} margin={{ top: 5, right: 16, bottom: 0, left: 0 }}>
                  <CartesianGrid strokeDasharray="3 3" stroke="#eef" />
                  <XAxis dataKey="date" hide />
                  <YAxis domain={[0, 100]} ticks={[0, 30, 50, 70, 100]} tick={{ fontSize: 10 }} width={36} />
                  <ReferenceLine y={70} stroke="#dc3545" strokeDasharray="3 3" />
                  <ReferenceLine y={30} stroke="#198754" strokeDasharray="3 3" />
                  <Tooltip
                    formatter={(value) => [Number(value).toFixed(1), 'RSI']}
                    labelFormatter={(label) => new Date(String(label)).toLocaleDateString()}
                  />
                  <Line type="monotone" dataKey="value" stroke="#6610f2" strokeWidth={1.5} dot={false} isAnimationActive={false} />
                </LineChart>
              </ResponsiveContainer>
            )}
          </>
        )}
      </Card.Body>
    </Card>
  )
}
