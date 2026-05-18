import { Routes, Route } from 'react-router-dom'
import { AppLayout } from './layouts/AppLayout'
import { RequireAuth } from './auth/RequireAuth'
import { RequireRole } from './auth/RequireRole'
import { DashboardPage } from './pages/DashboardPage'
import { PortfolioPage } from './pages/PortfolioPage'
import { StockDetailPage } from './pages/StockDetailPage'
import { StocksPage } from './pages/StocksPage'
import { WatchlistPage } from './pages/WatchlistPage'
import { AnalyticsPage } from './pages/AnalyticsPage'
import { ComparePage } from './pages/ComparePage'
import { OrdersPage } from './pages/OrdersPage'
import { ProfilePage } from './pages/ProfilePage'
import { TaxReportPage } from './pages/TaxReportPage'
import { DigestsPage } from './pages/DigestsPage'
import { AdminPage } from './pages/AdminPage'
import { LoginPage } from './pages/LoginPage'
import { RegisterPage } from './pages/RegisterPage'
import { NotFoundPage } from './pages/NotFoundPage'

export function AppRoutes() {
  return (
    <Routes>
      {/* Auth screens render outside the main layout shell. */}
      <Route path="/login" element={<LoginPage />} />
      <Route path="/register" element={<RegisterPage />} />

      <Route element={<AppLayout />}>
        {/* Stock browse + detail are anonymous-viewable; trade actions on the
            page gate behind auth themselves. */}
        <Route path="/stocks" element={<StocksPage />} />
        <Route path="/stocks/:symbol" element={<StockDetailPage />} />

        <Route element={<RequireAuth />}>
          <Route path="/" element={<DashboardPage />} />
          <Route path="/portfolio" element={<PortfolioPage />} />
          <Route path="/watchlist" element={<WatchlistPage />} />
          <Route path="/orders" element={<OrdersPage />} />
          <Route path="/analytics" element={<AnalyticsPage />} />
          <Route path="/compare" element={<ComparePage />} />
          <Route path="/profile" element={<ProfilePage />} />
          <Route path="/tax-report" element={<TaxReportPage />} />
          <Route path="/digests" element={<DigestsPage />} />
        </Route>

        <Route element={<RequireRole role="Admin" />}>
          <Route path="/admin" element={<AdminPage />} />
        </Route>

        <Route path="*" element={<NotFoundPage />} />
      </Route>
    </Routes>
  )
}
