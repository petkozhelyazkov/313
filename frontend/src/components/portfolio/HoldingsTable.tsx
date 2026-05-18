import { useState } from 'react'
import { Table, Dropdown, Card } from 'react-bootstrap'
import { Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import {
  formatCurrency,
  formatPercent,
  formatQty,
  plClassName,
  plSignedCurrency,
  plSignedPercent,
} from '../../lib/format'
import { SymbolLogo } from '../SymbolLogo'
import { Sparkline } from '../Sparkline'
import { useSparklines } from '../../api/sparklines'
import { TablePagination } from '../TablePagination'
import { usePagedData } from '../../hooks/usePagedData'
import { EditPositionModal } from './EditPositionModal'
import { LivePrice } from '../LivePrice'
import { useLivePrices } from '../../api/livePrices'
import type { PositionDto } from '../../api/portfolio'

type Props = {
  positions: PositionDto[]
  onTrade: (symbol: string, mode: 'buy' | 'sell') => void
}

function parseTags(raw: string | null): string[] {
  if (!raw) return []
  return raw
    .split(',')
    .map((s) => s.trim())
    .filter((s) => s.length > 0)
}

export function HoldingsTable({ positions, onTrade }: Props) {
  const { t } = useTranslation()
  const symbols = positions.map((p) => p.symbol)
  const { data: sparklines } = useSparklines(symbols)
  const paged = usePagedData(positions, { defaultPageSize: 25, storageKey: 'holdings' })
  const [editing, setEditing] = useState<PositionDto | null>(null)
  const live = useLivePrices(symbols)

  if (positions.length === 0) {
    return (
      <Card body className="text-center text-muted py-5">
        <p className="mb-2">{t('portfolio.noPositions')}</p>
      </Card>
    )
  }

  return (
    <Card>
      <Table responsive hover className="align-middle mb-0">
        <thead>
          <tr>
            <th>{t('portfolio.symbol')}</th>
            <th>{t('watchlist.thirtyDay')}</th>
            <th className="text-end">{t('common.quantity')}</th>
            <th className="text-end">{t('portfolio.avgCost')}</th>
            <th className="text-end">{t('orders.current')}</th>
            <th className="text-end">{t('portfolio.value')}</th>
            <th className="text-end">{t('portfolio.pl')}</th>
            <th className="text-end">%</th>
            <th className="text-end">{t('portfolio.weight')}</th>
            <th />
          </tr>
        </thead>
        <tbody>
          {paged.items.map((p) => {
            const tags = parseTags(p.tags)
            return (
            <tr key={p.symbol}>
              <td>
                <Link to={`/stocks/${encodeURIComponent(p.symbol)}`} className="text-decoration-none d-inline-flex align-items-center gap-2">
                  <SymbolLogo symbol={p.symbol} logoUrl={p.logoUrl} size={24} />
                  <span className="fw-semibold">{p.symbol}</span>
                  {p.name && <small className="text-muted d-none d-md-inline">{p.name}</small>}
                </Link>
                {(tags.length > 0 || p.notes) && (
                  <div className="position-meta mt-1 d-flex flex-wrap gap-1 align-items-center">
                    {tags.map((tag) => (
                      <span key={tag} className="position-tag-chip">{tag}</span>
                    ))}
                    {p.notes && (
                      <span className="text-muted small position-note-preview" title={p.notes}>
                        <i className="fi fi-rr-comment-alt-edit me-1" />
                        {p.notes.length > 60 ? `${p.notes.slice(0, 60)}…` : p.notes}
                      </span>
                    )}
                  </div>
                )}
              </td>
              <td><Sparkline points={sparklines?.[p.symbol]} /></td>
              <td className="text-end">{formatQty(p.quantity)}</td>
              <td className="text-end">{formatCurrency(p.averageCost)}</td>
              <td className="text-end">
                <LivePrice fallbackPrice={p.currentPrice} liveQuote={live[p.symbol]} />
              </td>
              <td className="text-end">{formatCurrency(p.currentValue)}</td>
              <td className={`text-end ${plClassName(p.unrealizedPl)}`}>{plSignedCurrency(p.unrealizedPl)}</td>
              <td className={`text-end ${plClassName(p.unrealizedPlPct)}`}>{plSignedPercent(p.unrealizedPlPct)}</td>
              <td className="text-end">{p.weight === null ? '—' : formatPercent(p.weight)}</td>
              <td className="text-end">
                <Dropdown align="end">
                  <Dropdown.Toggle variant="link" size="sm" className="p-0 text-secondary">
                    ⋯
                  </Dropdown.Toggle>
                  <Dropdown.Menu>
                    <Dropdown.Item onClick={() => onTrade(p.symbol, 'buy')}>{t('portfolio.buyMore')}</Dropdown.Item>
                    <Dropdown.Item onClick={() => onTrade(p.symbol, 'sell')}>{t('common.sell')}</Dropdown.Item>
                    <Dropdown.Divider />
                    <Dropdown.Item onClick={() => setEditing(p)}>{t('portfolio.editPosition')}</Dropdown.Item>
                    <Dropdown.Item as={Link} to={`/stocks/${encodeURIComponent(p.symbol)}`}>
                      {t('portfolio.viewDetails')}
                    </Dropdown.Item>
                  </Dropdown.Menu>
                </Dropdown>
              </td>
            </tr>
          )})}
        </tbody>
      </Table>

      <TablePagination
        page={paged.page}
        pageSize={paged.pageSize}
        totalCount={paged.total}
        onPageChange={paged.setPage}
        onPageSizeChange={paged.setPageSize}
      />
      <EditPositionModal position={editing} onClose={() => setEditing(null)} />
    </Card>
  )
}
