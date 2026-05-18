import { Card, Spinner } from 'react-bootstrap'
import { useTranslation } from 'react-i18next'
import { useAnalyst } from '../../api/stocks'
import { formatCurrency, plClassName, plSignedPercent } from '../../lib/format'

type Props = { symbol: string }

const RATING_COLORS: Record<string, string> = {
  strongBuy: '#198754',
  buy: '#5dbb6c',
  hold: '#ffc107',
  sell: '#fd7e14',
  strongSell: '#dc3545',
}

export function AnalystConsensusCard({ symbol }: Props) {
  const { t } = useTranslation()
  const { data, isLoading } = useAnalyst(symbol)

  if (isLoading) {
    return (
      <Card className="mb-3">
        <Card.Body className="text-center py-4"><Spinner size="sm" /></Card.Body>
      </Card>
    )
  }
  if (!data) return null

  const total = data.strongBuy + data.buy + data.hold + data.sell + data.strongSell
  if (total === 0) return null

  const segments = [
    { label: t('analyst.strongBuy'), value: data.strongBuy, color: RATING_COLORS.strongBuy },
    { label: t('analyst.buy'), value: data.buy, color: RATING_COLORS.buy },
    { label: t('analyst.hold'), value: data.hold, color: RATING_COLORS.hold },
    { label: t('analyst.sell'), value: data.sell, color: RATING_COLORS.sell },
    { label: t('analyst.strongSell'), value: data.strongSell, color: RATING_COLORS.strongSell },
  ]

  const verdictColor = data.recommendationMean === null
    ? 'text-muted'
    : data.recommendationMean <= 2.5
      ? 'text-success'
      : data.recommendationMean <= 3.5
        ? 'text-warning'
        : 'text-danger'

  return (
    <Card className="mb-3">
      <Card.Header className="d-flex justify-content-between align-items-center">
        <strong>{t('analyst.title')}</strong>
        <small className="text-muted">{t('analyst.count', { n: data.numAnalysts })}</small>
      </Card.Header>
      <Card.Body>
        <div className="mb-3 d-flex align-items-baseline gap-3 flex-wrap">
          <span className={`h4 mb-0 ${verdictColor}`}>{t(`analyst.verdict.${verdictLabelKey(data.verdictLabel)}`)}</span>
          {data.recommendationMean !== null && (
            <small className="text-muted">{t('analyst.mean')}: {data.recommendationMean.toFixed(2)} / 5</small>
          )}
        </div>

        <div className="analyst-bar mb-2">
          {segments.map((s) =>
            s.value === 0 ? null : (
              <div
                key={s.label}
                className="analyst-bar-seg"
                style={{ flex: s.value, background: s.color }}
                title={`${s.label}: ${s.value}`}
              >
                <span>{s.value}</span>
              </div>
            ),
          )}
        </div>
        <div className="d-flex justify-content-between flex-wrap gap-2 small text-muted mb-3">
          {segments.map((s) => (
            <span key={s.label} className="d-inline-flex align-items-center gap-1">
              <span className="analyst-dot" style={{ background: s.color }} />
              {s.label}
            </span>
          ))}
        </div>

        {data.targetMean !== null && (
          <div className="analyst-target">
            <div className="text-uppercase text-muted small fw-semibold mb-1">{t('analyst.priceTarget')}</div>
            <div className="d-flex justify-content-between align-items-baseline flex-wrap gap-2">
              <div>
                <strong className="fs-5">{formatCurrency(data.targetMean)}</strong>{' '}
                <small className="text-muted">{t('analyst.mean')}</small>
              </div>
              <div className="small text-muted">
                {t('analyst.range', {
                  low: data.targetLow ? formatCurrency(data.targetLow) : '—',
                  high: data.targetHigh ? formatCurrency(data.targetHigh) : '—',
                })}
              </div>
              {data.upsidePct !== null && (
                <div className={plClassName(data.upsidePct)}>
                  {plSignedPercent(data.upsidePct)} {t('analyst.fromCurrent')}
                </div>
              )}
            </div>
          </div>
        )}
      </Card.Body>
    </Card>
  )
}

function verdictLabelKey(label: string): string {
  switch (label) {
    case 'Strong Buy': return 'strongBuy'
    case 'Buy': return 'buy'
    case 'Hold': return 'hold'
    case 'Sell': return 'sell'
    case 'Strong Sell': return 'strongSell'
    default: return 'unknown'
  }
}
