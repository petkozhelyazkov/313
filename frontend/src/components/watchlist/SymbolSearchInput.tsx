import { useEffect, useRef, useState } from 'react'
import { Form, ListGroup, Spinner } from 'react-bootstrap'
import { useTranslation } from 'react-i18next'
import { useSymbolSearch, type StockSearchResult } from '../../api/stocks'

type Props = {
  onSelect: (symbol: string) => void
  disabled?: boolean
}

export function SymbolSearchInput({ onSelect, disabled }: Props) {
  const { t } = useTranslation()
  const [query, setQuery] = useState('')
  const [debouncedQuery, setDebouncedQuery] = useState('')
  const [open, setOpen] = useState(false)
  const containerRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    const t = setTimeout(() => setDebouncedQuery(query.trim()), 250)
    return () => clearTimeout(t)
  }, [query])

  const { data, isLoading } = useSymbolSearch(debouncedQuery, open && debouncedQuery.length >= 1)

  useEffect(() => {
    const handler = (e: MouseEvent) => {
      if (containerRef.current && !containerRef.current.contains(e.target as Node)) {
        setOpen(false)
      }
    }
    document.addEventListener('mousedown', handler)
    return () => document.removeEventListener('mousedown', handler)
  }, [])

  const handlePick = (s: StockSearchResult) => {
    onSelect(s.symbol)
    setQuery('')
    setDebouncedQuery('')
    setOpen(false)
  }

  return (
    <div ref={containerRef} className="position-relative" style={{ maxWidth: 400 }}>
      <Form.Control
        type="search"
        placeholder={t('search.placeholder')}
        value={query}
        onChange={(e) => {
          setQuery(e.target.value)
          setOpen(true)
        }}
        onFocus={() => setOpen(true)}
        disabled={disabled}
        autoComplete="off"
      />
      {open && debouncedQuery.length > 0 && (
        <div className="position-absolute w-100 mt-1 shadow-sm" style={{ zIndex: 1050 }}>
          {isLoading ? (
            <ListGroup>
              <ListGroup.Item className="text-center text-muted">
                <Spinner animation="border" size="sm" /> {t('search.searching')}
              </ListGroup.Item>
            </ListGroup>
          ) : data && data.length > 0 ? (
            <ListGroup>
              {data.slice(0, 8).map((s) => (
                <ListGroup.Item
                  key={`${s.symbol}-${s.exchange ?? ''}`}
                  action
                  onClick={() => handlePick(s)}
                  className="d-flex justify-content-between align-items-center"
                >
                  <div>
                    <strong>{s.symbol}</strong>{' '}
                    <small className="text-muted">{s.name}</small>
                  </div>
                  <small className="text-muted">
                    {s.exchange} · {s.currency}
                  </small>
                </ListGroup.Item>
              ))}
            </ListGroup>
          ) : (
            <ListGroup>
              <ListGroup.Item className="text-center text-muted small">
                {t('search.noMatches', { query: debouncedQuery })}
              </ListGroup.Item>
            </ListGroup>
          )}
        </div>
      )}
    </div>
  )
}
