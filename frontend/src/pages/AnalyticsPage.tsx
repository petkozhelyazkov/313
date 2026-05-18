import { useState } from 'react'
import { Container, Row, Col } from 'react-bootstrap'
import { useTranslation } from 'react-i18next'
import { useDocumentTitle } from '../hooks/useDocumentTitle'
import type { AnalyticsRange } from '../api/analytics'
import { PerformanceChart } from '../components/analytics/PerformanceChart'
import { AllocationPie } from '../components/analytics/AllocationPie'
import { SectorPie } from '../components/analytics/SectorPie'
import { ReturnsBarChart } from '../components/analytics/ReturnsBarChart'
import { RiskMetricsCard } from '../components/analytics/RiskMetricsCard'
import { DiversificationCard } from '../components/analytics/DiversificationCard'
import { AdvancedMetricsCard } from '../components/analytics/AdvancedMetricsCard'

export function AnalyticsPage() {
  const { t } = useTranslation()
  useDocumentTitle(t('analytics.title'))
  const [range, setRange] = useState<AnalyticsRange>('1Y')

  return (
    <Container className="py-4">
      <h1 className="h3 mb-3">{t('analytics.title')}</h1>

      <PerformanceChart range={range} onRangeChange={setRange} />

      <div className="mb-3">
        <AdvancedMetricsCard />
      </div>

      <Row className="g-3 mb-3">
        <Col lg={6}>
          <RiskMetricsCard />
        </Col>
        <Col lg={6}>
          <ReturnsBarChart />
        </Col>
      </Row>

      <Row className="g-3 mb-3">
        <Col lg={6}>
          <AllocationPie />
        </Col>
        <Col lg={6}>
          <SectorPie />
        </Col>
      </Row>

      <Row className="g-3">
        <Col lg={12}>
          <DiversificationCard />
        </Col>
      </Row>
    </Container>
  )
}
