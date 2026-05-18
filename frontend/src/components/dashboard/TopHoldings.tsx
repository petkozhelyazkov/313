import { Card, Table, Placeholder } from 'react-bootstrap'
import { Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import {
  formatCurrency,
  formatPercent,
  formatQty,
  plClassName,
  plSignedCurrency,
} from '../../lib/format'
import { SymbolLogo } from '../SymbolLogo'
import type { PositionDto } from '../../api/portfolio'

type Props = {
  positions?: PositionDto[]
  isLoading: boolean
}

export function TopHoldings({ positions, isLoading }: Props) {
  const { t } = useTranslation()
  const top = (positions ?? [])
    .filter((p) => !p.isClosed)
    .sort((a, b) => (b.weight ?? 0) - (a.weight ?? 0))
    .slice(0, 5)

  return (
    <Card>
      <Card.Header>
        <strong>{t('dashboard.topHoldings')}</strong>
      </Card.Header>
      <Card.Body className="p-0">
        {isLoading ? (
          <div className="p-3">
            <Placeholder animation="glow"><Placeholder xs={12} /></Placeholder>
            <Placeholder animation="glow"><Placeholder xs={10} /></Placeholder>
            <Placeholder animation="glow"><Placeholder xs={8} /></Placeholder>
          </div>
        ) : top.length === 0 ? (
          <div className="p-4 text-center text-muted">
            <p className="mb-2">{t('dashboard.noHoldings')}</p>
            <Link to="/stocks/AAPL">{t('dashboard.searchStock')}</Link> {t('dashboard.makeFirstTrade')}
          </div>
        ) : (
          <Table responsive hover className="mb-0 align-middle">
            <thead>
              <tr>
                <th>{t('portfolio.symbol')}</th>
                <th className="text-end">{t('common.quantity')}</th>
                <th className="text-end">{t('portfolio.value')}</th>
                <th className="text-end">{t('portfolio.pl')}</th>
                <th className="text-end">{t('portfolio.weight')}</th>
              </tr>
            </thead>
            <tbody>
              {top.map((p) => (
                <tr key={p.symbol}>
                  <td>
                    <Link to={`/stocks/${encodeURIComponent(p.symbol)}`} className="text-decoration-none d-inline-flex align-items-center gap-2">
                      <SymbolLogo symbol={p.symbol} logoUrl={p.logoUrl} size={24} />
                      <span className="fw-semibold">{p.symbol}</span>
                    </Link>
                  </td>
                  <td className="text-end">{formatQty(p.quantity)}</td>
                  <td className="text-end">{formatCurrency(p.currentValue)}</td>
                  <td className={`text-end ${plClassName(p.unrealizedPl)}`}>
                    {plSignedCurrency(p.unrealizedPl)}
                  </td>
                  <td className="text-end">{p.weight === null ? '—' : formatPercent(p.weight)}</td>
                </tr>
              ))}
            </tbody>
          </Table>
        )}
      </Card.Body>
    </Card>
  )
}
