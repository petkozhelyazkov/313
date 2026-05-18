import { useEffect, useMemo, useRef, useState } from 'react'
import { Modal, Form } from 'react-bootstrap'
import { useNavigate } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { useDebounce } from '../hooks/useDebounce'
import { useSymbolSearch } from '../api/stocks'
import { useAuth } from '../auth/useAuth'
import { useTheme, type Theme } from '../theme/ThemeContext'
import { SUPPORTED_LANGUAGES } from '../i18n'
import { SymbolLogo } from './SymbolLogo'
import { Icon } from './Icon'

type Props = {
  open: boolean
  onClose: () => void
}

type Action = {
  id: string
  group: string
  label: string
  hint?: string
  icon?: string
  logoSymbol?: string
  logoUrl?: string | null
  onSelect: () => void
}

export function CommandPalette({ open, onClose }: Props) {
  const { t, i18n } = useTranslation()
  const navigate = useNavigate()
  const { isAuthenticated, hasRole } = useAuth()
  const isAdmin = hasRole('Admin')
  const { theme, setTheme } = useTheme()
  const [query, setQuery] = useState('')
  const [activeIndex, setActiveIndex] = useState(0)
  const inputRef = useRef<HTMLInputElement>(null)
  const listRef = useRef<HTMLDivElement>(null)
  const debounced = useDebounce(query, 200)
  const { data: searchResults } = useSymbolSearch(debounced, debounced.trim().length >= 1 && open)

  useEffect(() => {
    if (open) {
      setQuery('')
      setActiveIndex(0)
      setTimeout(() => inputRef.current?.focus(), 50)
    }
  }, [open])

  const actions = useMemo<Action[]>(() => {
    const navActions: Action[] = []
    if (isAuthenticated) {
      navActions.push(
        { id: 'nav-dashboard', group: t('palette.group.navigate'), label: t('nav.dashboard'), icon: 'rr-apps', onSelect: () => navigate('/') },
        { id: 'nav-portfolio', group: t('palette.group.navigate'), label: t('nav.portfolio'), icon: 'rr-briefcase', onSelect: () => navigate('/portfolio') },
        { id: 'nav-orders', group: t('palette.group.navigate'), label: t('nav.orders'), icon: 'rr-list', onSelect: () => navigate('/orders') },
        { id: 'nav-watchlist', group: t('palette.group.navigate'), label: t('nav.watchlist'), icon: 'rr-star', onSelect: () => navigate('/watchlist') },
        { id: 'nav-analytics', group: t('palette.group.navigate'), label: t('nav.analytics'), icon: 'rr-chart-pie', onSelect: () => navigate('/analytics') },
        { id: 'nav-compare', group: t('palette.group.navigate'), label: t('nav.compare'), icon: 'rr-chart-line-up', onSelect: () => navigate('/compare') },
        { id: 'nav-profile', group: t('palette.group.navigate'), label: t('nav.profile'), icon: 'rr-user', onSelect: () => navigate('/profile') },
      )
    }
    navActions.push({
      id: 'nav-stocks',
      group: t('palette.group.navigate'),
      label: t('nav.stocks'),
      icon: 'rr-search',
      onSelect: () => navigate('/stocks'),
    })
    if (isAdmin) {
      navActions.push({
        id: 'nav-admin',
        group: t('palette.group.navigate'),
        label: t('nav.admin'),
        icon: 'rr-shield',
        onSelect: () => navigate('/admin'),
      })
    }

    const themeActions: Action[] = (['light', 'dark', 'system'] as Theme[]).map((value) => ({
      id: `theme-${value}`,
      group: t('palette.group.theme'),
      label: t(`palette.theme.${value}`),
      icon: value === 'light' ? 'rr-sun' : value === 'dark' ? 'rr-moon' : 'rr-computer',
      hint: theme === value ? '✓' : undefined,
      onSelect: () => {
        setTheme(value)
        onClose()
      },
    }))

    const langActions: Action[] = SUPPORTED_LANGUAGES.map((l) => ({
      id: `lang-${l.code}`,
      group: t('palette.group.language'),
      label: l.label,
      hint: i18n.language === l.code ? '✓' : undefined,
      icon: 'rr-globe',
      onSelect: () => {
        i18n.changeLanguage(l.code)
        onClose()
      },
    }))

    const tradeActions: Action[] = []
    if (isAuthenticated) {
      tradeActions.push({
        id: 'action-deposit',
        group: t('palette.group.actions'),
        label: t('palette.action.deposit'),
        icon: 'rr-money-bill-wave',
        onSelect: () => navigate('/profile#cash'),
      })
    }

    return [...navActions, ...tradeActions, ...themeActions, ...langActions]
  }, [t, i18n, navigate, isAuthenticated, isAdmin, theme, setTheme, onClose])

  const symbolActions = useMemo<Action[]>(() => {
    if (!searchResults) return []
    return searchResults.map((s) => ({
      id: `symbol-${s.symbol}`,
      group: t('palette.group.stocks'),
      label: s.symbol,
      hint: s.name,
      logoSymbol: s.symbol,
      logoUrl: s.logoUrl,
      onSelect: () => {
        navigate(`/stocks/${encodeURIComponent(s.symbol)}`)
        onClose()
      },
    }))
  }, [searchResults, navigate, onClose, t])

  const filtered = useMemo<Action[]>(() => {
    const q = query.trim().toLowerCase()
    const base = q.length === 0
      ? actions
      : actions.filter((a) =>
          a.label.toLowerCase().includes(q) ||
          (a.hint?.toLowerCase().includes(q) ?? false),
        )
    return [...symbolActions, ...base]
  }, [actions, symbolActions, query])

  const grouped = useMemo(() => {
    const map = new Map<string, Action[]>()
    for (const a of filtered) {
      if (!map.has(a.group)) map.set(a.group, [])
      map.get(a.group)!.push(a)
    }
    return Array.from(map.entries())
  }, [filtered])

  const flatList = filtered

  useEffect(() => {
    setActiveIndex(0)
  }, [query, searchResults])

  useEffect(() => {
    const el = listRef.current?.querySelector<HTMLElement>(`[data-cmd-index="${activeIndex}"]`)
    el?.scrollIntoView({ block: 'nearest' })
  }, [activeIndex])

  const handleKey = (e: React.KeyboardEvent) => {
    if (e.key === 'ArrowDown') {
      e.preventDefault()
      setActiveIndex((i) => Math.min(i + 1, flatList.length - 1))
    } else if (e.key === 'ArrowUp') {
      e.preventDefault()
      setActiveIndex((i) => Math.max(i - 1, 0))
    } else if (e.key === 'Enter') {
      e.preventDefault()
      const item = flatList[activeIndex]
      if (item) {
        item.onSelect()
        if (item.id.startsWith('nav-') || item.id.startsWith('symbol-') || item.id.startsWith('action-')) {
          onClose()
        }
      }
    }
  }

  let runningIndex = -1

  return (
    <Modal show={open} onHide={onClose} centered className="cmd-palette-modal" backdrop>
      <div className="cmd-palette">
        <div className="cmd-palette-input-wrap">
          <Icon name="rr-search" className="cmd-palette-search-icon" />
          <Form.Control
            ref={inputRef}
            type="text"
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            onKeyDown={handleKey}
            placeholder={t('palette.placeholder')}
            className="cmd-palette-input"
            autoComplete="off"
          />
          <kbd className="cmd-palette-kbd">ESC</kbd>
        </div>
        <div className="cmd-palette-list" ref={listRef}>
          {flatList.length === 0 ? (
            <div className="cmd-palette-empty">{t('palette.empty')}</div>
          ) : (
            grouped.map(([group, items]) => (
              <div key={group} className="cmd-palette-group">
                <div className="cmd-palette-group-header">{group}</div>
                {items.map((item) => {
                  runningIndex++
                  const isActive = runningIndex === activeIndex
                  const idx = runningIndex
                  return (
                    <button
                      key={item.id}
                      type="button"
                      data-cmd-index={idx}
                      className={`cmd-palette-item ${isActive ? 'is-active' : ''}`}
                      onClick={() => {
                        item.onSelect()
                        if (item.id.startsWith('nav-') || item.id.startsWith('symbol-') || item.id.startsWith('action-')) {
                          onClose()
                        }
                      }}
                      onMouseEnter={() => setActiveIndex(idx)}
                    >
                      <span className="cmd-palette-item-icon">
                        {item.logoSymbol ? (
                          <SymbolLogo symbol={item.logoSymbol} logoUrl={item.logoUrl ?? null} size={20} />
                        ) : item.icon ? (
                          <Icon name={item.icon} />
                        ) : null}
                      </span>
                      <span className="cmd-palette-item-label">{item.label}</span>
                      {item.hint && <span className="cmd-palette-item-hint">{item.hint}</span>}
                    </button>
                  )
                })}
              </div>
            ))
          )}
        </div>
        <div className="cmd-palette-footer">
          <span><kbd>↑</kbd><kbd>↓</kbd> {t('palette.navigate')}</span>
          <span><kbd>⏎</kbd> {t('palette.select')}</span>
          <span><kbd>ESC</kbd> {t('palette.close')}</span>
        </div>
      </div>
    </Modal>
  )
}
