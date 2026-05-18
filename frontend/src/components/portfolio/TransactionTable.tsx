import { useState } from 'react'
import { Table, Form, Card, Dropdown } from 'react-bootstrap'
import { useTranslation } from 'react-i18next'
import { Button } from '../Button'
import { TablePagination } from '../TablePagination'
import { usePersistedNumber } from '../../hooks/usePersistedNumber'
import { Link } from 'react-router-dom'
import { useTransactions, type TransactionDto } from '../../api/portfolio'
import { formatCurrency, formatQty, plClassName, plSignedCurrency } from '../../lib/format'
import { EditTransactionModal } from './EditTransactionModal'
import { TagPlSummaryCard } from './TagPlSummaryCard'

function parseTags(raw: string | null): string[] {
  if (!raw) return []
  return raw.split(',').map((s) => s.trim()).filter((s) => s.length > 0)
}

export function TransactionTable() {
  const { t } = useTranslation()
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = usePersistedNumber('transactions', 20)
  const [symbolFilter, setSymbolFilter] = useState('')
  const [activeFilter, setActiveFilter] = useState<string | undefined>(undefined)
  const [tagFilter, setTagFilter] = useState<string | undefined>(undefined)
  const [editing, setEditing] = useState<TransactionDto | null>(null)

  const { data, isLoading, isError } = useTransactions(page, pageSize, activeFilter, tagFilter)

  const applyFilter = (e: React.FormEvent) => {
    e.preventDefault()
    setPage(1)
    setActiveFilter(symbolFilter.trim() ? symbolFilter.trim().toUpperCase() : undefined)
  }

  return (
    <div>
      <TagPlSummaryCard
        activeTag={tagFilter}
        onSelectTag={(tag) => {
          setTagFilter(tag)
          setPage(1)
        }}
      />

      <Form className="d-flex gap-2 mb-3 align-items-end" onSubmit={applyFilter}>
        <Form.Group>
          <Form.Label className="small mb-1">{t('portfolio.filterBySymbol')}</Form.Label>
          <Form.Control
            size="sm"
            placeholder="e.g. AAPL"
            value={symbolFilter}
            onChange={(e) => setSymbolFilter(e.target.value)}
            style={{ width: 200 }}
          />
        </Form.Group>
        <Button size="sm" type="submit" variant="primary">{t('common.apply')}</Button>
        {(activeFilter || tagFilter) && (
          <Button
            size="sm"
            variant="ghost"
            type="button"
            onClick={() => {
              setSymbolFilter('')
              setActiveFilter(undefined)
              setTagFilter(undefined)
              setPage(1)
            }}
          >
            {t('common.clear')}
          </Button>
        )}
      </Form>

      {isError ? (
        <Card body>{t('portfolio.couldNotLoad')}</Card>
      ) : isLoading || !data ? (
        <Card body className="text-muted">{t('common.loading')}</Card>
      ) : data.items.length === 0 ? (
        <Card body className="text-center text-muted py-4">{t('portfolio.noTransactions')}</Card>
      ) : (
        <Card>
          <Table responsive hover className="align-middle mb-0">
            <thead>
              <tr>
                <th>{t('portfolio.date')}</th>
                <th>{t('portfolio.type')}</th>
                <th>{t('portfolio.symbol')}</th>
                <th>{t('portfolio.tags')}</th>
                <th className="text-end">{t('common.quantity')}</th>
                <th className="text-end">{t('common.price')}</th>
                <th className="text-end">{t('common.total')}</th>
                <th className="text-end">{t('portfolio.realizedPl')}</th>
                <th />
              </tr>
            </thead>
            <tbody>
              {data.items.map((tx) => {
                const tags = parseTags(tx.tags)
                return (
                <tr key={tx.id}>
                  <td className="small">{new Date(tx.executedAt).toLocaleString()}</td>
                  <td>
                    <span className={`badge ${tx.type === 'Buy' ? 'bg-success' : 'bg-danger'}`}>
                      {t(tx.type === 'Buy' ? 'common.buy' : 'common.sell')}
                    </span>
                  </td>
                  <td>
                    <Link to={`/stocks/${encodeURIComponent(tx.symbol)}`} className="fw-semibold text-decoration-none">
                      {tx.symbol}
                    </Link>
                  </td>
                  <td>
                    <div className="d-flex flex-wrap gap-1">
                      {tags.map((tag) => (
                        <button
                          key={tag}
                          type="button"
                          className="position-tag-chip"
                          onClick={() => {
                            setTagFilter(tag)
                            setPage(1)
                          }}
                          title={t('portfolio.filterByTag', { tag })}
                        >
                          {tag}
                        </button>
                      ))}
                    </div>
                    {tx.notes && (
                      <div className="text-muted small mt-1 position-note-preview" title={tx.notes}>
                        {tx.notes.length > 60 ? `${tx.notes.slice(0, 60)}…` : tx.notes}
                      </div>
                    )}
                  </td>
                  <td className="text-end">{formatQty(tx.quantity)}</td>
                  <td className="text-end">{formatCurrency(tx.pricePerShare)}</td>
                  <td className="text-end">{formatCurrency(tx.totalAmount)}</td>
                  <td className={`text-end ${plClassName(tx.realizedPl)}`}>
                    {tx.realizedPl === null ? '—' : plSignedCurrency(tx.realizedPl)}
                  </td>
                  <td className="text-end">
                    <Dropdown align="end">
                      <Dropdown.Toggle variant="link" size="sm" className="p-0 text-secondary">
                        ⋯
                      </Dropdown.Toggle>
                      <Dropdown.Menu>
                        <Dropdown.Item onClick={() => setEditing(tx)}>{t('portfolio.editTransaction')}</Dropdown.Item>
                      </Dropdown.Menu>
                    </Dropdown>
                  </td>
                </tr>
              )})}
            </tbody>
          </Table>

          <TablePagination
            page={page}
            pageSize={pageSize}
            totalCount={data.totalCount}
            onPageChange={setPage}
            onPageSizeChange={(s) => { setPageSize(s); setPage(1) }}
          />
        </Card>
      )}

      <EditTransactionModal transaction={editing} onClose={() => setEditing(null)} />
    </div>
  )
}
