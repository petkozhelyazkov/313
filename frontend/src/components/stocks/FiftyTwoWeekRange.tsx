import { useTranslation } from 'react-i18next'
import { formatCurrency } from '../../lib/format'

type Props = {
  current: number | null | undefined
  high: number | null | undefined
  low: number | null | undefined
}

/**
 * Horizontal bar showing where the current price sits within its 52-week range.
 * Returns null if data is missing — caller decides what to render in that case.
 */
export function FiftyTwoWeekRange({ current, high, low }: Props) {
  const { t } = useTranslation()
  if (
    current == null ||
    high == null ||
    low == null ||
    high <= low ||
    Number.isNaN(current) ||
    Number.isNaN(high) ||
    Number.isNaN(low)
  ) {
    return null
  }

  const clamped = Math.min(high, Math.max(low, current))
  const percent = ((clamped - low) / (high - low)) * 100

  return (
    <div className="mt-2" style={{ minWidth: 220 }}>
      <div className="d-flex justify-content-between small text-muted mb-1">
        <span>{t('stock.fiftyTwoWeekLow')}</span>
        <span>{t('stock.fiftyTwoWeekHigh')}</span>
      </div>
      <div className="position-relative" style={{ height: 8 }}>
        <div
          className="position-absolute w-100 rounded"
          style={{
            top: 0,
            bottom: 0,
            background: 'linear-gradient(to right, #dc3545, #ffc107, #198754)',
            opacity: 0.5,
          }}
        />
        <div
          className="position-absolute bg-dark rounded-circle"
          style={{
            top: -3,
            width: 14,
            height: 14,
            left: `calc(${percent}% - 7px)`,
            border: '2px solid white',
            boxShadow: '0 0 2px rgba(0,0,0,0.4)',
          }}
        />
      </div>
      <div className="d-flex justify-content-between small mt-1">
        <strong>{formatCurrency(low)}</strong>
        <span className="text-muted">{formatCurrency(current)}</span>
        <strong>{formatCurrency(high)}</strong>
      </div>
    </div>
  )
}
