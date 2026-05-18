import { Container, Row, Col } from 'react-bootstrap'
import { Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { useDocumentTitle } from '../hooks/useDocumentTitle'
import { usePortfolioSummary } from '../api/portfolio'
import { useAuth } from '../auth/useAuth'
import { SummaryCards } from '../components/dashboard/SummaryCards'
import { TopHoldings } from '../components/dashboard/TopHoldings'
import { Hotlist } from '../components/dashboard/Hotlist'
import { MiniPerformanceChart } from '../components/dashboard/MiniPerformanceChart'
import { EarningsCalendar } from '../components/dashboard/EarningsCalendar'
import { MarketMovers } from '../components/dashboard/MarketMovers'
import { DividendsWidget } from '../components/dashboard/DividendsWidget'

export function DashboardPage() {
  const { user } = useAuth()
  const { data: summary, isLoading } = usePortfolioSummary()
  const { t } = useTranslation()
  useDocumentTitle(t('nav.dashboard'))

  return (
    <Container className="py-4">
      <div className="d-flex flex-column flex-md-row justify-content-between align-items-md-center mb-3 gap-2">
        <div>
          <h1 className="h3 mb-1">{t('dashboard.title')}</h1>
          <p className="text-muted mb-0">
            {t('dashboard.welcome', { name: user?.displayName || user?.email })}
          </p>
        </div>
        <div className="d-flex gap-2">
          <Link to="/portfolio" className="btn btn-outline-primary btn-sm">
            {t('nav.portfolio')}
          </Link>
          <Link to="/watchlist" className="btn btn-outline-primary btn-sm">
            {t('nav.watchlist')}
          </Link>
          <Link to="/analytics" className="btn btn-outline-primary btn-sm">
            {t('nav.analytics')}
          </Link>
        </div>
      </div>

      <SummaryCards summary={summary} isLoading={isLoading} />

      <Row className="g-3">
        <Col lg={8}>
          <TopHoldings positions={summary?.positions} isLoading={isLoading} />
          <div className="mt-3">
            <MiniPerformanceChart />
          </div>
          <div className="mt-3">
            <DividendsWidget />
          </div>
          <div className="mt-3">
            <MarketMovers />
          </div>
        </Col>
        <Col lg={4}>
          <EarningsCalendar />
          <div className="mt-3">
            <Hotlist />
          </div>
        </Col>
      </Row>
    </Container>
  )
}
