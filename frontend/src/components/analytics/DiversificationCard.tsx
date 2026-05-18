import { Card, ProgressBar, Spinner } from 'react-bootstrap'
import { useTranslation } from 'react-i18next'
import { useAnalyticsDiversification } from '../../api/analytics'

function scoreVariant(score: number): 'success' | 'info' | 'warning' | 'danger' {
  if (score >= 80) return 'success'
  if (score >= 60) return 'info'
  if (score >= 40) return 'warning'
  return 'danger'
}

export function DiversificationCard() {
  const { t } = useTranslation()
  const { data, isLoading, error } = useAnalyticsDiversification()

  return (
    <Card>
      <Card.Body>
        <Card.Title className="d-flex justify-content-between align-items-center">
          <span>{t('analytics.diversification.title')}</span>
          {data && (
            <span className={`badge bg-${scoreVariant(data.score)}`}>{data.score}/100</span>
          )}
        </Card.Title>

        {isLoading && (
          <div className="text-center py-3">
            <Spinner animation="border" size="sm" />
          </div>
        )}

        {error && <div className="text-danger small">{t('analytics.diversification.failed')}</div>}

        {data && (
          <>
            <ProgressBar
              now={data.score}
              variant={scoreVariant(data.score)}
              className="mb-3"
              style={{ height: '10px' }}
            />

            <p className="text-muted small mb-3">{data.verdict}</p>

            <div className="row g-2 small mb-3">
              <div className="col-6">
                <div className="text-muted">{t('analytics.diversification.positions')}</div>
                <div className="fw-semibold">{data.positionsCount}</div>
              </div>
              <div className="col-6">
                <div className="text-muted">{t('analytics.diversification.sectors')}</div>
                <div className="fw-semibold">{data.sectorsCount}</div>
              </div>
              <div className="col-6">
                <div className="text-muted">{t('analytics.diversification.largestPosition')}</div>
                <div className="fw-semibold">{data.largestPositionPct.toFixed(1)}%</div>
              </div>
              <div className="col-6">
                <div className="text-muted">{t('analytics.diversification.largestSector')}</div>
                <div className="fw-semibold">{data.largestSectorPct.toFixed(1)}%</div>
              </div>
            </div>

            {data.suggestions.length > 0 && (
              <div>
                <div className="small text-muted mb-1">{t('analytics.diversification.suggestions')}</div>
                <ul className="small mb-0 ps-3">
                  {data.suggestions.map((s, i) => (
                    <li key={i}>{s}</li>
                  ))}
                </ul>
              </div>
            )}
          </>
        )}
      </Card.Body>
    </Card>
  )
}
