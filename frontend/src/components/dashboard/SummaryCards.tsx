import { Card, Col, Row, Placeholder } from 'react-bootstrap'
import { useTranslation } from 'react-i18next'
import {
  formatCurrency,
  plClassName,
  plSignedCurrency,
  plSignedPercent,
} from '../../lib/format'
import type { PortfolioSummary } from '../../api/portfolio'

type Props = {
  summary?: PortfolioSummary
  isLoading: boolean
}

export function SummaryCards({ summary, isLoading }: Props) {
  const { t } = useTranslation()
  return (
    <Row className="g-3 mb-4">
      <Col md={6} lg={3}>
        <Card>
          <Card.Body>
            <div className="text-muted small text-uppercase mb-1">{t('dashboard.totalValue')}</div>
            {isLoading || !summary ? (
              <Placeholder as="div" animation="glow" className="h3"><Placeholder xs={6} /></Placeholder>
            ) : (
              <h2 className="h3 mb-0">{formatCurrency(summary.totalValue)}</h2>
            )}
          </Card.Body>
        </Card>
      </Col>
      <Col md={6} lg={3}>
        <Card>
          <Card.Body>
            <div className="text-muted small text-uppercase mb-1">{t('dashboard.freeFunds')}</div>
            {isLoading || !summary ? (
              <Placeholder as="div" animation="glow" className="h3"><Placeholder xs={6} /></Placeholder>
            ) : (
              <h2 className="h3 mb-0">{formatCurrency(summary.cashBalance)}</h2>
            )}
          </Card.Body>
        </Card>
      </Col>
      <Col md={6} lg={3}>
        <Card>
          <Card.Body>
            <div className="text-muted small text-uppercase mb-1">{t('dashboard.invested')}</div>
            {isLoading || !summary ? (
              <Placeholder as="div" animation="glow" className="h3"><Placeholder xs={6} /></Placeholder>
            ) : (
              <h2 className="h3 mb-0">{formatCurrency(summary.totalInvested)}</h2>
            )}
          </Card.Body>
        </Card>
      </Col>
      <Col md={6} lg={3}>
        <Card>
          <Card.Body>
            <div className="text-muted small text-uppercase mb-1">{t('dashboard.result')}</div>
            {isLoading || !summary ? (
              <Placeholder as="div" animation="glow" className="h3"><Placeholder xs={6} /></Placeholder>
            ) : (
              <>
                <h2 className={`h3 mb-0 ${plClassName(summary.unrealizedPl)}`}>
                  {plSignedCurrency(summary.unrealizedPl)}
                </h2>
                <small className={plClassName(summary.unrealizedPl)}>
                  {plSignedPercent(summary.unrealizedPlPct)}
                </small>
              </>
            )}
          </Card.Body>
        </Card>
      </Col>
    </Row>
  )
}
