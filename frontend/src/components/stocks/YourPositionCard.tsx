import { Card, Row, Col } from 'react-bootstrap'
import { useTranslation } from 'react-i18next'
import {
  formatCurrency,
  formatQty,
  plClassName,
  plSignedCurrency,
  plSignedPercent,
} from '../../lib/format'
import type { PositionDto } from '../../api/portfolio'

type Props = {
  position: PositionDto
}

export function YourPositionCard({ position }: Props) {
  const { t } = useTranslation()
  return (
    <Card className="mb-3">
      <Card.Header><strong>{t('stock.yourPosition')}</strong></Card.Header>
      <Card.Body>
        <Row className="g-3">
          <Col xs={6} md={3}>
            <div className="text-muted small text-uppercase">{t('common.quantity')}</div>
            <strong>{formatQty(position.quantity)}</strong>
          </Col>
          <Col xs={6} md={3}>
            <div className="text-muted small text-uppercase">{t('portfolio.avgCost')}</div>
            <strong>{formatCurrency(position.averageCost)}</strong>
          </Col>
          <Col xs={6} md={3}>
            <div className="text-muted small text-uppercase">{t('stock.currentValue')}</div>
            <strong>{formatCurrency(position.currentValue)}</strong>
          </Col>
          <Col xs={6} md={3}>
            <div className="text-muted small text-uppercase">{t('portfolio.unrealizedPl')}</div>
            <strong className={plClassName(position.unrealizedPl)}>
              {plSignedCurrency(position.unrealizedPl)}{' '}
              <small>({plSignedPercent(position.unrealizedPlPct)})</small>
            </strong>
          </Col>
        </Row>
      </Card.Body>
    </Card>
  )
}
