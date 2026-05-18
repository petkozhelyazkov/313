import { Card, ProgressBar, Spinner } from 'react-bootstrap'
import { useTranslation } from 'react-i18next'
import { useMyAchievements, type Achievement } from '../api/users'
import { Icon } from './Icon'

export function AchievementsCard() {
  const { t } = useTranslation()
  const { data, isLoading } = useMyAchievements()

  if (isLoading) {
    return (
      <Card>
        <Card.Header><strong>{t('achievements.title')}</strong></Card.Header>
        <Card.Body className="text-center py-3">
          <Spinner animation="border" size="sm" />
        </Card.Body>
      </Card>
    )
  }

  if (!data || data.length === 0) return null

  const earned = data.filter((a) => a.earned).length

  return (
    <Card>
      <Card.Header className="d-flex justify-content-between align-items-center">
        <strong>{t('achievements.title')}</strong>
        <span className="badge bg-secondary">{earned}/{data.length}</span>
      </Card.Header>
      <Card.Body>
        <div className="row g-2">
          {data.map((a) => (
            <div key={a.code} className="col-md-6 col-lg-4">
              <BadgeTile achievement={a} />
            </div>
          ))}
        </div>
      </Card.Body>
    </Card>
  )
}

function BadgeTile({ achievement: a }: { achievement: Achievement }) {
  const { t } = useTranslation()
  const showProgress = !a.earned && a.progress !== null && a.target !== null && a.target > 0
  const progressPct = showProgress ? Math.min(100, ((a.progress ?? 0) / (a.target ?? 1)) * 100) : 0

  return (
    <div className={`border rounded p-2 h-100 ${a.earned ? 'border-success bg-success-subtle' : 'opacity-75'}`}>
      <div className="d-flex align-items-start gap-2">
        <div
          className={a.earned ? 'text-success' : 'text-muted'}
          style={{ filter: a.earned ? 'none' : 'grayscale(1)' }}
        >
          <Icon name={a.icon} size={28} />
        </div>
        <div className="flex-grow-1 min-w-0">
          <div className="fw-semibold small">{a.name}</div>
          <div className="text-muted" style={{ fontSize: '0.75rem' }}>{a.description}</div>
          {a.earned && a.earnedAt && (
            <div className="text-success" style={{ fontSize: '0.7rem' }}>
              <Icon name="rr-check" className="me-1" />
              {t('achievements.earned', { date: new Date(a.earnedAt).toLocaleDateString() })}
            </div>
          )}
          {showProgress && (
            <div className="mt-1">
              <ProgressBar now={progressPct} style={{ height: '4px' }} />
              <div className="text-muted text-end" style={{ fontSize: '0.7rem' }}>
                {a.progress} / {a.target}
              </div>
            </div>
          )}
        </div>
      </div>
    </div>
  )
}
