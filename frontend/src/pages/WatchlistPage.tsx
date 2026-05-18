import { useMemo, useState } from 'react'
import { Container, Card, Spinner, Nav, Modal, Form } from 'react-bootstrap'
import { useTranslation, Trans } from 'react-i18next'
import {
  useWatchlist,
  useWatchlistLists,
  useAddToWatchlist,
  useRemoveFromWatchlist,
  useRenameWatchlist,
  useDeleteWatchlist,
} from '../api/watchlist'
import { Button, ButtonGroup } from '../components/Button'
import { SymbolSearchInput } from '../components/watchlist/SymbolSearchInput'
import { WatchlistTable } from '../components/watchlist/WatchlistTable'
import { TradeModal } from '../components/trade/TradeModal'
import { useTabFromUrl } from '../hooks/useTabFromUrl'
import { useDocumentTitle } from '../hooks/useDocumentTitle'
import { useConfirm } from '../components/ConfirmDialog'
import { toast } from '../lib/toast'
import { getApiErrorMessage } from '../api/client'

type TradeState = { symbol: string; mode: 'buy' | 'sell' } | null

export function WatchlistPage() {
  const { t } = useTranslation()
  useDocumentTitle(t('watchlist.title'))
  const [activeList, setActiveList] = useTabFromUrl('list', 'Default')
  const { data: lists } = useWatchlistLists()
  const { data, isLoading } = useWatchlist(activeList)
  const add = useAddToWatchlist()
  const remove = useRemoveFromWatchlist()
  const rename = useRenameWatchlist()
  const del = useDeleteWatchlist()
  const confirm = useConfirm()
  const [trade, setTrade] = useState<TradeState>(null)
  const [renameOpen, setRenameOpen] = useState(false)
  const [createOpen, setCreateOpen] = useState(false)

  const tabs = useMemo(() => {
    const base = lists ?? []
    if (!base.some((b) => b.listName === activeList)) {
      return [...base, { listName: activeList, count: 0 }]
    }
    return base
  }, [lists, activeList])

  const handleAdd = async (symbol: string) => {
    try {
      const item = await add.mutateAsync({ symbol, listName: activeList })
      toast.success(t('toast.addedToList', { symbol: item.symbol, list: activeList }))
    } catch (err) {
      toast.error(getApiErrorMessage(err))
    }
  }

  const handleRemove = async (symbol: string) => {
    try {
      await remove.mutateAsync({ symbol, listName: activeList })
      toast.success(t('toast.removedFromList', { symbol, list: activeList }))
    } catch (err) {
      toast.error(getApiErrorMessage(err))
    }
  }

  const handleDeleteList = async () => {
    if (activeList === 'Default') return
    const ok = await confirm({
      title: t('watchlist.lists.deleteTitle'),
      body: <Trans i18nKey="watchlist.lists.deleteBody" values={{ list: activeList }} />,
      confirmLabel: t('common.delete'),
      cancelLabel: t('common.cancel'),
      variant: 'danger',
    })
    if (!ok) return
    try {
      await del.mutateAsync(activeList)
      toast.success(t('toast.deletedList', { list: activeList }))
      setActiveList('Default')
    } catch (err) {
      toast.error(getApiErrorMessage(err))
    }
  }

  const countLabel = data
    ? (data.length === 1
      ? t('watchlist.symbolIn', { count: data.length, list: activeList })
      : t('watchlist.symbolsIn', { count: data.length, list: activeList }))
    : ''

  return (
    <Container className="py-4">
      <div className="d-flex justify-content-between align-items-center mb-3 flex-wrap gap-2">
        <h1 className="h3 mb-0">{t('watchlist.title')}</h1>
        {data && <div className="text-muted small">{countLabel}</div>}
      </div>

      <div className="d-flex align-items-center gap-2 mb-3 flex-wrap">
        <Nav variant="tabs" activeKey={activeList} onSelect={(k) => k && setActiveList(k)} className="flex-grow-1">
          {tabs.map((tab) => (
            <Nav.Item key={tab.listName}>
              <Nav.Link eventKey={tab.listName}>
                {tab.listName} <span className="text-muted small">({tab.count})</span>
              </Nav.Link>
            </Nav.Item>
          ))}
        </Nav>
        <ButtonGroup size="sm">
          <Button variant="primary" outline size="sm" onClick={() => setCreateOpen(true)}>
            {t('watchlist.lists.new')}
          </Button>
          <Button variant="secondary" outline size="sm" onClick={() => setRenameOpen(true)} disabled={activeList === 'Default'}>
            {t('watchlist.lists.rename')}
          </Button>
          <Button variant="danger" outline size="sm" onClick={handleDeleteList} disabled={activeList === 'Default'} loading={del.isPending}>
            {t('watchlist.lists.delete')}
          </Button>
        </ButtonGroup>
      </div>

      <Card className="mb-3">
        <Card.Body>
          <div className="text-muted small mb-2">{t('watchlist.addSymbolTo', { list: activeList })}</div>
          <SymbolSearchInput onSelect={handleAdd} disabled={add.isPending} />
        </Card.Body>
      </Card>

      {isLoading || !data ? (
        <Card body className="text-center">
          <Spinner animation="border" size="sm" /> {t('common.loadingDot')}…
        </Card>
      ) : (
        <WatchlistTable
          items={data}
          onTrade={(symbol, mode) => setTrade({ symbol, mode })}
          onRemove={handleRemove}
          isRemoving={remove.isPending}
        />
      )}

      {trade && (
        <TradeModal
          open={trade !== null}
          onClose={() => setTrade(null)}
          symbol={trade.symbol}
          mode={trade.mode}
        />
      )}

      <ListNameModal
        show={createOpen}
        title={t('watchlist.lists.newTitle')}
        confirmLabel={t('watchlist.lists.createBtn')}
        onCancel={() => setCreateOpen(false)}
        onConfirm={(name) => {
          setActiveList(name)
          setCreateOpen(false)
          toast.success(t('toast.switchedToList', { list: name }))
        }}
      />

      <ListNameModal
        show={renameOpen}
        title={t('watchlist.lists.renameTitle', { list: activeList })}
        confirmLabel={t('watchlist.lists.renameBtn')}
        initial={activeList === 'Default' ? '' : activeList}
        onCancel={() => setRenameOpen(false)}
        onConfirm={async (newName) => {
          try {
            await rename.mutateAsync({ oldName: activeList, newName })
            setActiveList(newName)
            setRenameOpen(false)
            toast.success(t('toast.renamedList', { list: newName }))
          } catch (err) {
            toast.error(getApiErrorMessage(err))
          }
        }}
      />
    </Container>
  )
}

function ListNameModal({
  show, title, confirmLabel, initial = '', onCancel, onConfirm,
}: {
  show: boolean
  title: string
  confirmLabel: string
  initial?: string
  onCancel: () => void
  onConfirm: (name: string) => void
}) {
  const { t } = useTranslation()
  const [name, setName] = useState(initial)
  const trimmed = name.trim()
  const valid = trimmed.length > 0 && trimmed.length <= 50 && trimmed !== 'Default'

  return (
    <Modal show={show} onHide={onCancel} centered onShow={() => setName(initial)}>
      <Modal.Header closeButton>
        <Modal.Title>{title}</Modal.Title>
      </Modal.Header>
      <Form
        onSubmit={(e) => {
          e.preventDefault()
          if (valid) onConfirm(trimmed)
        }}
      >
        <Modal.Body>
          <Form.Group>
            <Form.Label>{t('watchlist.lists.listName')}</Form.Label>
            <Form.Control
              value={name}
              onChange={(e) => setName(e.target.value)}
              autoFocus
              maxLength={50}
              placeholder={t('watchlist.lists.listNamePlaceholder')}
            />
            <Form.Text className="text-muted">{t('watchlist.lists.listNameHint')}</Form.Text>
          </Form.Group>
        </Modal.Body>
        <Modal.Footer>
          <Button variant="secondary" outline onClick={onCancel}>{t('common.cancel')}</Button>
          <Button type="submit" variant="primary" disabled={!valid}>{confirmLabel}</Button>
        </Modal.Footer>
      </Form>
    </Modal>
  )
}
