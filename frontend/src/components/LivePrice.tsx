import { useEffect, useRef, useState } from 'react'
import { formatCurrency } from '../lib/format'
import type { LiveQuote } from '../api/livePrices'

type Props = {
  fallbackPrice: number | null | undefined
  liveQuote?: LiveQuote
  className?: string
}

/**
 * Renders a price that briefly flashes green/red when a new tick arrives.
 * Falls back to `fallbackPrice` (REST cache) until a tick is received.
 */
export function LivePrice({ fallbackPrice, liveQuote, className }: Props) {
  const [flash, setFlash] = useState<'up' | 'down' | null>(null)
  const prevPriceRef = useRef<number | null>(null)
  const price = liveQuote?.price ?? fallbackPrice ?? null

  useEffect(() => {
    if (liveQuote === undefined || price === null) return
    const prev = prevPriceRef.current
    if (prev !== null && prev !== price) {
      setFlash(price > prev ? 'up' : 'down')
      const t = setTimeout(() => setFlash(null), 600)
      return () => clearTimeout(t)
    }
    prevPriceRef.current = price
  }, [liveQuote, price])

  useEffect(() => {
    if (price !== null && prevPriceRef.current === null) {
      prevPriceRef.current = price
    }
  }, [price])

  return (
    <span className={`live-price ${flash === 'up' ? 'live-price-up' : ''} ${flash === 'down' ? 'live-price-down' : ''} ${className ?? ''}`}>
      {formatCurrency(price)}
      {liveQuote && <span className="live-dot" aria-hidden="true" />}
    </span>
  )
}
