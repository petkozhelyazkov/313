import { useEffect, useRef, useState } from 'react'
import { HubConnection, HubConnectionBuilder, HubConnectionState, LogLevel } from '@microsoft/signalr'
import { getAccessToken } from '../auth/tokenStorage'

export type LiveQuote = {
  symbol: string
  price: number
  dayChange: number | null
  dayChangePct: number | null
  previousClose: number | null
  volume: number
  fetchedAt: string
  isStale: boolean
}

let sharedConnection: HubConnection | null = null
let sharedTokenSnapshot: string | null = null
const symbolListeners = new Map<string, Set<(q: LiveQuote) => void>>()
const symbolRefcount = new Map<string, number>()

async function ensureConnected(): Promise<HubConnection | null> {
  const token = getAccessToken()
  if (!token) return null

  if (sharedConnection && sharedTokenSnapshot === token && sharedConnection.state === HubConnectionState.Connected) {
    return sharedConnection
  }

  // Rebuild if the token changed or connection died.
  if (sharedConnection) {
    try { await sharedConnection.stop() } catch { /* ignore */ }
    sharedConnection = null
  }

  const conn = new HubConnectionBuilder()
    .withUrl(`/hub/prices?access_token=${encodeURIComponent(token)}`)
    .withAutomaticReconnect()
    .configureLogging(LogLevel.Warning)
    .build()

  conn.on('priceUpdate', (quotes: LiveQuote[]) => {
    for (const q of quotes) {
      const listeners = symbolListeners.get(q.symbol)
      if (listeners) {
        listeners.forEach((cb) => cb(q))
      }
    }
  })

  try {
    await conn.start()
  } catch {
    return null
  }
  sharedConnection = conn
  sharedTokenSnapshot = token

  // Re-subscribe to anything that was already requested before the connection came up.
  const allSyms = Array.from(symbolRefcount.keys())
  if (allSyms.length > 0) {
    try { await conn.invoke('Subscribe', allSyms) } catch { /* ignore */ }
  }
  return conn
}

async function addSubscription(symbols: string[]): Promise<void> {
  const toAdd: string[] = []
  for (const s of symbols) {
    const sym = s.toUpperCase()
    const c = symbolRefcount.get(sym) ?? 0
    symbolRefcount.set(sym, c + 1)
    if (c === 0) toAdd.push(sym)
  }
  if (toAdd.length === 0) return
  const conn = await ensureConnected()
  if (conn?.state === HubConnectionState.Connected) {
    try { await conn.invoke('Subscribe', toAdd) } catch { /* ignore */ }
  }
}

async function removeSubscription(symbols: string[]): Promise<void> {
  const toRemove: string[] = []
  for (const s of symbols) {
    const sym = s.toUpperCase()
    const c = symbolRefcount.get(sym) ?? 0
    if (c <= 1) {
      symbolRefcount.delete(sym)
      toRemove.push(sym)
    } else {
      symbolRefcount.set(sym, c - 1)
    }
  }
  if (toRemove.length === 0 || !sharedConnection) return
  if (sharedConnection.state === HubConnectionState.Connected) {
    try { await sharedConnection.invoke('Unsubscribe', toRemove) } catch { /* ignore */ }
  }
}

/**
 * Subscribe to live price ticks for a set of symbols. Returns the most recent quote
 * received per symbol since mount, keyed by uppercase symbol. Re-subscribes
 * automatically if the symbol list changes.
 */
export function useLivePrices(symbols: string[]): Record<string, LiveQuote> {
  const [snapshot, setSnapshot] = useState<Record<string, LiveQuote>>({})
  const symbolsKey = symbols.map((s) => s.toUpperCase()).sort().join(',')
  const lastKeyRef = useRef<string>('')

  useEffect(() => {
    if (symbolsKey === lastKeyRef.current) return
    const prev = lastKeyRef.current ? lastKeyRef.current.split(',') : []
    const next = symbolsKey ? symbolsKey.split(',') : []
    const toAdd = next.filter((s) => !prev.includes(s))
    const toRemove = prev.filter((s) => !next.includes(s))
    lastKeyRef.current = symbolsKey

    const listener = (q: LiveQuote) => {
      setSnapshot((s) => ({ ...s, [q.symbol]: q }))
    }
    for (const sym of toAdd) {
      let set = symbolListeners.get(sym)
      if (!set) {
        set = new Set()
        symbolListeners.set(sym, set)
      }
      set.add(listener)
    }
    for (const sym of toRemove) {
      const set = symbolListeners.get(sym)
      if (set) {
        set.delete(listener)
        if (set.size === 0) symbolListeners.delete(sym)
      }
    }
    if (toAdd.length > 0) void addSubscription(toAdd)
    if (toRemove.length > 0) void removeSubscription(toRemove)

    return () => {
      // Drop listeners on unmount. The effect re-runs on key change too,
      // but we don't need to clean those up here since toRemove already handled it.
      for (const sym of next) {
        const set = symbolListeners.get(sym)
        if (set) {
          set.delete(listener)
          if (set.size === 0) symbolListeners.delete(sym)
        }
      }
      void removeSubscription(next)
    }
  }, [symbolsKey])

  return snapshot
}
