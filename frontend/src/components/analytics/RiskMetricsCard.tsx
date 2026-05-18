import { Card, Row, Col, Spinner, OverlayTrigger, Tooltip } from 'react-bootstrap'
import { useTranslation } from 'react-i18next'
import { useAnalyticsRisk } from '../../api/analytics'
import { plClassName } from '../../lib/format'

type Metric = {
  label: string
  value: string
  className?: string
  hint: string
}

export function RiskMetricsCard() {
  const { t } = useTranslation()
  const { data, isLoading, isError } = useAnalyticsRisk()

  let metrics: Metric[] | null = null
  let footnote: string | null = null
  if (data) {
    if (data.dataPoints < 5) {
      footnote = t('analytics.risk.needSnapshots', { count: data.dataPoints })
    } else {
      metrics = [
        {
          label: t('analytics.risk.portfolioBeta'),
          value: data.beta == null ? '—' : data.beta.toFixed(2),
          hint: t('analytics.risk.betaHint'),
        },
        {
          label: t('analytics.risk.annualVolatility'),
          value: data.annualizedVolatility == null ? '—' : data.annualizedVolatility.toFixed(2) + '%',
          hint: t('analytics.risk.volatilityHint'),
        },
        {
          label: t('analytics.risk.sharpeRatio'),
          value: data.sharpeRatio == null ? '—' : data.sharpeRatio.toFixed(2),
          className: plClassName(data.sharpeRatio),
          hint: t('analytics.risk.sharpeHint'),
        },
        {
          label: t('analytics.risk.maxDrawdownLabel'),
          value: data.maxDrawdown == null ? '—' : '−' + data.maxDrawdown.toFixed(1) + '%',
          className: 'text-danger',
          hint: t('analytics.risk.drawdownHint'),
        },
      ]
    }
  }

  return (
    <Card className="h-100">
      <Card.Header><strong>{t('analytics.risk.title')}</strong></Card.Header>
      <Card.Body>
        {isLoading ? (
          <div className="text-center py-4"><Spinner animation="border" size="sm" /></div>
        ) : isError || !data ? (
          <div className="text-muted text-center py-4 small">{t('analytics.couldNotLoadRisk')}</div>
        ) : footnote ? (
          <div className="text-muted text-center py-4 small">{footnote}</div>
        ) : metrics ? (
          <Row className="g-3">
            {metrics.map((m) => (
              <Col xs={6} key={m.label}>
                <OverlayTrigger placement="top" overlay={<Tooltip>{m.hint}</Tooltip>}>
                  <div>
                    <div className="text-muted small text-uppercase">{m.label}</div>
                    <strong className={['fs-4', m.className].filter(Boolean).join(' ')}>
                      {m.value}
                    </strong>
                  </div>
                </OverlayTrigger>
              </Col>
            ))}
          </Row>
        ) : null}
      </Card.Body>
    </Card>
  )
}
