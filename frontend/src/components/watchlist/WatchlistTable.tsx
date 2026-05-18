import { useState } from 'react'
import { Card, Table } from 'react-bootstrap'
import { useTranslation } from 'react-i18next'
import { Button, ButtonGroup } from '../Button'
import { Link } from 'react-router-dom'
import {
  formatCurrency,
  plClassName,
  plSignedCurrency,
  plSignedPercent,
} from '../../lib/format'
import { SymbolLogo } from '../SymbolLogo'
import { Sparkline } from '../Sparkline'
import { useSparklines } from '../../api/sparklines'
import { TablePagination } from '../TablePagination'
import { usePagedData } from '../../hooks/usePagedData'
import type { WatchlistItemDto } from '../../api/watchlist'

type Props = {
  items: WatchlistItemDto[]
  onTrade: (symbol: string, mode: 'buy' | 'sell') => void
  onRemove: (symbol: string) => void
  isRemoving: boolean
}

export function WatchlistTable({ items, onTrade, onRemove, isRemoving }: Props) {
  const { t } = useTranslation()
  const [confirmRemove, setConfirmRemove] = useState<string | null>(null)
  const { data: sparklines } = useSparklines(items.map((i) => i.symbol))
  const paged = usePagedData(items, { defaultPageSize: 25, storageKey: 'watchlist' })

  if (items.length === 0) {
    return (
      <Card body className="text-center text-muted py-5">
        {t('watchlist.empty')}
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
            <th>{t('watchlist.notes')}</th>
            <th className="text-end">{t('common.price')}</th>
            <th className="text-end">{t('watchlist.dayChange')}</th>
            <th>{t('watchlist.added')}</th>
            <th />
          </tr>
        </thead>
        <tbody>
          {paged.items.map((item) => (
            <tr key={item.id}>
              <td>
                <Link to={`/stocks/${encodeURIComponent(item.symbol)}`} className="text-decoration-none d-inline-flex align-items-center gap-2">
                  <SymbolLogo symbol={item.symbol} logoUrl={item.logoUrl} size={24} />
                  <span className="fw-semibold">{item.symbol}</span>
                  {item.name && <small className="text-muted d-none d-md-inline">{item.name}</small>}
                </Link>
              </td>
              <td><Sparkline points={sparklines?.[item.symbol]} /></td>
              <td className="text-muted small">{item.notes || '—'}</td>
              <td className="text-end">
                {item.quote ? formatCurrency(item.quote.price) : '—'}
                {item.quote?.isStale && <div><small className="text-warning">{t('common.stale')}</small></div>}
              </td>
              <td className={`text-end ${plClassName(item.quote?.dayChange)}`}>
                {item.quote ? (
                  <>
                    {plSignedCurrency(item.quote.dayChange)}
                    <div><small>{plSignedPercent(item.quote.dayChangePct)}</small></div>
                  </>
                ) : '—'}
              </td>
              <td className="small text-muted">{new Date(item.addedAt).toLocaleDateString()}</td>
              <td className="text-end">
                <ButtonGroup size="sm">
                  <Button size="sm" variant="success" outline onClick={() => onTrade(item.symbol, 'buy')}>{t('common.buy')}</Button>
                  <Button size="sm" variant="danger" outline onClick={() => onTrade(item.symbol, 'sell')}>{t('common.sell')}</Button>
                  {confirmRemove === item.symbol ? (
                    <>
                      <Button
                        size="sm"
                        variant="danger"
                        onClick={() => {
                          onRemove(item.symbol)
                          setConfirmRemove(null)
                        }}
                        disabled={isRemoving}
                      >
                        {t('common.confirm')}
                      </Button>
                      <Button size="sm" variant="secondary" onClick={() => setConfirmRemove(null)}>
                        {t('common.cancel')}
                      </Button>
                    </>
                  ) : (
                    <Button size="sm" variant="secondary" outline onClick={() => setConfirmRemove(item.symbol)}>
                      {t('common.remove')}
                    </Button>
                  )}
                </ButtonGroup>
              </td>
            </tr>
          ))}
        </tbody>
      </Table>

      <TablePagination
        page={paged.page}
        pageSize={paged.pageSize}
        totalCount={paged.total}
        onPageChange={paged.setPage}
        onPageSizeChange={paged.setPageSize}
      />
    </Card>
  )
}
