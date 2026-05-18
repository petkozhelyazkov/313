import { useState } from 'react'
import { Container, Card, Spinner, Form } from 'react-bootstrap'
import { useTranslation } from 'react-i18next'
import { Button, ButtonGroup } from '../components/Button'
import { useQueries } from '@tanstack/react-query'
import {
  ResponsiveContainer,
  LineChart,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  Legend,
  ReferenceLine,
} from 'recharts'
import { apiClient } from '../api/client'
import type { HistoryResponse, Range } from '../api/stocks'
import { SymbolSearchInput } from '../components/watchlist/SymbolSearchInput'
import { SymbolLogo } from '../components/SymbolLogo'
import { useDocumentTitle } from '../hooks/useDocumentTitle'

const RANGES: Range[] = ['1M', '3M', '6M', '1Y', '5Y', 'MAX']
const COLORS = ['#0d6efd', '#198754', '#dc3545', '#fd7e14']
const MAX_SYMBOLS = 4

export function ComparePage() {
  const { t } = useTranslation()
  useDocumentTitle(t('compare.title'))
  const [symbols, setSymbols] = useState<string[]>(['AAPL', 'MSFT'])
  const [range, setRange] = useState<Range>('1Y')

  const queries = useQueries({
    queries: symbols.map((symbol) => ({
      queryKey: ['compare', symbol, range],
      queryFn: async () => {
        const res = await apiClient.get<HistoryResponse>(`/api/stocks/${encodeURIComponent(symbol)}/history`, {
          params: { range },
        })
        return res.data
      },
      staleTime: 60 * 60_000,
    })),
  })

  const anyLoading = queries.some((q) => q.isLoading)
  const allReady = queries.length > 0 && queries.every((q) => q.data)

  // Build aligned chart data: union of dates, normalized to 100 at first point.
  type Row = { date: string; [symbol: string]: number | string | null }
  let chartData: Row[] = []
  if (allReady) {
    const seriesByDate = new Map<string, Row>()
    queries.forEach((q, idx) => {
      const data = q.data!
      const points = data.points
      if (points.length === 0) return
      const startClose = points[0].close
      for (const p of points) {
        const normalized = startClose === 0 ? 100 : (p.close / startClose) * 100
        let row = seriesByDate.get(p.date)
        if (!row) {
          row = { date: p.date }
          seriesByDate.set(p.date, row)
        }
        row[symbols[idx]] = normalized
      }
    })
    chartData = Array.from(seriesByDate.values()).sort((a, b) =>
      String(a.date).localeCompare(String(b.date)),
    )
  }

  const handleAdd = (sym: string) => {
    const upper = sym.toUpperCase()
    if (symbols.includes(upper) || symbols.length >= MAX_SYMBOLS) return
    setSymbols([...symbols, upper])
  }

  const handleRemove = (sym: string) => {
    if (symbols.length <= 1) return
    setSymbols(symbols.filter((s) => s !== sym))
  }

  return (
    <Container className="py-4">
      <div className="d-flex justify-content-between align-items-center mb-3 flex-wrap gap-2">
        <h1 className="h3 mb-0">{t('compare.title')}</h1>
        <ButtonGroup size="sm">
          {RANGES.map((r) => (
            <Button
              key={r}
              variant="primary"
              outline={r !== range}
              size="sm"
              onClick={() => setRange(r)}
            >
              {r}
            </Button>
          ))}
        </ButtonGroup>
      </div>

      <Card className="mb-3">
        <Card.Body>
          <Form.Label className="small text-muted text-uppercase">
            {t('compare.symbols')} ({symbols.length}/{MAX_SYMBOLS})
          </Form.Label>
          <div className="d-flex flex-wrap gap-2 mb-3">
            {symbols.map((s, idx) => (
              <span
                key={s}
                className="compare-symbol-chip d-flex align-items-center gap-2 px-2 py-2 border rounded"
              >
                <SymbolLogo symbol={s} size={20} />
                <span style={{ color: COLORS[idx % COLORS.length] }}>●</span>
                <strong>{s}</strong>
                {symbols.length > 1 && (
                  <Button
                    size="sm"
                    variant="danger"
                    outline
                    className="px-2 py-0"
                    onClick={() => handleRemove(s)}
                    title={t('common.remove')}
                  >
                    ×
                  </Button>
                )}
              </span>
            ))}
          </div>
          {symbols.length < MAX_SYMBOLS && (
            <>
              <Form.Label className="small text-muted">{t('compare.addAnother')}</Form.Label>
              <SymbolSearchInput onSelect={handleAdd} />
            </>
          )}
        </Card.Body>
      </Card>

      <Card>
        <Card.Header><strong>{t('compare.performance')}</strong></Card.Header>
        <Card.Body style={{ minHeight: 380 }}>
          {anyLoading ? (
            <div className="d-flex justify-content-center align-items-center" style={{ minHeight: 320 }}>
              <Spinner animation="border" />
            </div>
          ) : !allReady || chartData.length === 0 ? (
            <div className="text-center text-muted py-5">{t('compare.noData')}</div>
          ) : (
            <ResponsiveContainer width="100%" height={360}>
              <LineChart data={chartData} margin={{ top: 5, right: 16, bottom: 0, left: 0 }}>
                <CartesianGrid strokeDasharray="3 3" stroke="#eef" />
                <XAxis
                  dataKey="date"
                  tick={{ fontSize: 12 }}
                  tickFormatter={(d) => new Date(d).toLocaleDateString('en-US', { month: 'short', year: '2-digit' })}
                  minTickGap={60}
                />
                <YAxis
                  domain={['auto', 'auto']}
                  tickFormatter={(v) => Math.round(Number(v)).toString()}
                  tick={{ fontSize: 12 }}
                  width={50}
                />
                <Tooltip
                  formatter={(value, name) => [
                    `${Number(value).toFixed(2)} (${((Number(value) - 100) / 100 * 100).toFixed(1)}%)`,
                    String(name),
                  ]}
                  labelFormatter={(label) => new Date(String(label)).toLocaleDateString()}
                />
                <Legend />
                <ReferenceLine y={100} stroke="#888" strokeDasharray="3 3" />
                {symbols.map((sym, idx) => (
                  <Line
                    key={sym}
                    type="monotone"
                    dataKey={sym}
                    stroke={COLORS[idx % COLORS.length]}
                    strokeWidth={2}
                    dot={false}
                    connectNulls
                    isAnimationActive={false}
                  />
                ))}
              </LineChart>
            </ResponsiveContainer>
          )}
        </Card.Body>
      </Card>
    </Container>
  )
}
