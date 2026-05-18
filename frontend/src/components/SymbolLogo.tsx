import { useState } from 'react'

type Props = {
  symbol: string
  logoUrl?: string | null
  size?: number
  className?: string
}

// Deterministic color per symbol so monogram fallbacks look distinct.
const PALETTE = [
  '#0d6efd', '#198754', '#dc3545', '#fd7e14', '#6f42c1',
  '#20c997', '#0dcaf0', '#d63384', '#6610f2', '#0a58ca',
]

function colorFor(symbol: string): string {
  let hash = 0
  for (let i = 0; i < symbol.length; i++) hash = (hash * 31 + symbol.charCodeAt(i)) >>> 0
  return PALETTE[hash % PALETTE.length]
}

export function SymbolLogo({ symbol, logoUrl, size = 28, className }: Props) {
  const [errored, setErrored] = useState(false)
  const fallbackColor = colorFor(symbol)
  const initials = symbol.slice(0, symbol.startsWith('AAPL') ? 2 : 2).toUpperCase()

  const style = {
    width: size,
    height: size,
    fontSize: size * 0.4,
    background: fallbackColor,
    flexShrink: 0,
  } as const

  if (logoUrl && !errored) {
    return (
      <img
        src={logoUrl}
        alt={symbol}
        width={size}
        height={size}
        onError={() => setErrored(true)}
        className={['rounded bg-white border', className].filter(Boolean).join(' ')}
        style={{ objectFit: 'contain', padding: 2, flexShrink: 0 }}
      />
    )
  }

  return (
    <div
      className={[
        'rounded text-white d-inline-flex align-items-center justify-content-center fw-semibold',
        className,
      ]
        .filter(Boolean)
        .join(' ')}
      style={style}
      aria-label={symbol}
      title={symbol}
    >
      {initials}
    </div>
  )
}
