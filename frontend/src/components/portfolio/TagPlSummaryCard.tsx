import { Card } from 'react-bootstrap'
import { useTranslation } from 'react-i18next'
import { useTagPlSummary } from '../../api/portfolio'
import { plClassName, plSignedCurrency } from '../../lib/format'

type Props = {
  activeTag?: string
  onSelectTag: (tag: string | undefined) => void
}

export function TagPlSummaryCard({ activeTag, onSelectTag }: Props) {
  const { t } = useTranslation()
  const { data, isLoading } = useTagPlSummary()

  if (isLoading) return null
  if (!data || data.length === 0) return null

  return (
    <Card className="mb-3">
      <Card.Header className="d-flex justify-content-between align-items-center">
        <strong>{t('portfolio.tagBreakdown')}</strong>
        {activeTag && (
          <button
            type="button"
            className="btn btn-sm btn-link p-0"
            onClick={() => onSelectTag(undefined)}
          >
            {t('common.clear')}
          </button>
        )}
      </Card.Header>
      <Card.Body className="d-flex flex-wrap gap-2">
        {data.map((row) => {
          const isActive = activeTag === row.tag
          return (
            <button
              key={row.tag}
              type="button"
              onClick={() => onSelectTag(isActive ? undefined : row.tag)}
              className={`tag-summary-chip ${isActive ? 'is-active' : ''}`}
            >
              <span className="tag-summary-name">{row.tag}</span>
              <span className={`tag-summary-pl ${plClassName(row.realizedPl)}`}>
                {plSignedCurrency(row.realizedPl)}
              </span>
              <span className="tag-summary-count">· {row.transactionCount}</span>
            </button>
          )
        })}
      </Card.Body>
    </Card>
  )
}
