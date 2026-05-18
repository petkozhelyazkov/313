import { Card, Row, Col } from 'react-bootstrap'
import { useTranslation } from 'react-i18next'
import { formatLargeCurrency, formatLargeNumber, formatPercent } from '../../lib/format'
import type { CompanyProfileResponse } from '../../api/profile'

type Props = {
  profile: CompanyProfileResponse
}

export function CompanyProfileCard({ profile }: Props) {
  const { t } = useTranslation()
  const stats: { label: string; value: string }[] = [
    { label: t('stock.marketCap'), value: formatLargeCurrency(profile.marketCap) },
    { label: t('stock.peRatio'), value: profile.peRatio == null ? '—' : profile.peRatio.toFixed(2) },
    { label: t('stock.eps'), value: profile.eps == null ? '—' : '$' + profile.eps.toFixed(2) },
    { label: t('stock.dividendYield'), value: profile.dividendYield == null ? '—' : formatPercent(profile.dividendYield * 100) },
    { label: t('stock.beta'), value: profile.beta == null ? '—' : profile.beta.toFixed(2) },
    { label: t('stock.employees'), value: profile.employees == null ? '—' : formatLargeNumber(profile.employees) },
  ]

  const hasBasics = profile.sector || profile.industry || profile.website || profile.description
  if (!hasBasics && stats.every((s) => s.value === '—')) return null

  return (
    <Card className="mb-3">
      <Card.Header><strong>{t('stock.about', { name: profile.name ?? profile.symbol })}</strong></Card.Header>
      <Card.Body>
        <Row className="g-3">
          <Col lg={7}>
            <div className="d-flex flex-wrap gap-3 mb-2 small">
              {profile.sector && (
                <div>
                  <div className="text-muted text-uppercase">{t('stock.sector')}</div>
                  <div>{profile.sector}</div>
                </div>
              )}
              {profile.industry && (
                <div>
                  <div className="text-muted text-uppercase">{t('stock.industry')}</div>
                  <div>{profile.industry}</div>
                </div>
              )}
              {profile.ceo && (
                <div>
                  <div className="text-muted text-uppercase">{t('stock.ceo')}</div>
                  <div>{profile.ceo}</div>
                </div>
              )}
              {profile.website && (
                <div>
                  <div className="text-muted text-uppercase">{t('stock.website')}</div>
                  <div>
                    <a href={profile.website} target="_blank" rel="noopener noreferrer">
                      {profile.website.replace(/^https?:\/\//, '').replace(/\/$/, '')}
                    </a>
                  </div>
                </div>
              )}
            </div>
            {profile.description && (
              <p className="mb-0 small text-muted" style={{ maxHeight: 160, overflow: 'auto' }}>
                {profile.description}
              </p>
            )}
          </Col>
          <Col lg={5}>
            <Row className="g-2">
              {stats.map((s) => (
                <Col xs={6} key={s.label}>
                  <Card body className="py-2 px-3 border-0 profile-stat">
                    <div className="text-muted small text-uppercase">{s.label}</div>
                    <strong>{s.value}</strong>
                  </Card>
                </Col>
              ))}
            </Row>
          </Col>
        </Row>
      </Card.Body>
    </Card>
  )
}
