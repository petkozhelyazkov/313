const currencyFormatter = new Intl.NumberFormat('en-US', {
  style: 'currency',
  currency: 'USD',
  minimumFractionDigits: 2,
  maximumFractionDigits: 2,
})

const compactCurrency = new Intl.NumberFormat('en-US', {
  style: 'currency',
  currency: 'USD',
  notation: 'compact',
  minimumFractionDigits: 1,
  maximumFractionDigits: 1,
})

const percentFormatter = new Intl.NumberFormat('en-US', {
  style: 'percent',
  minimumFractionDigits: 2,
  maximumFractionDigits: 2,
})

const quantityFormatter = new Intl.NumberFormat('en-US', {
  minimumFractionDigits: 0,
  maximumFractionDigits: 6,
})

export function formatCurrency(value: number | null | undefined): string {
  if (value === null || value === undefined || Number.isNaN(value)) return '—'
  return currencyFormatter.format(value)
}

export function formatCompactCurrency(value: number | null | undefined): string {
  if (value === null || value === undefined || Number.isNaN(value)) return '—'
  return compactCurrency.format(value)
}

export function formatPercent(value: number | null | undefined): string {
  if (value === null || value === undefined || Number.isNaN(value)) return '—'
  return percentFormatter.format(value / 100)
}

export function formatQty(value: number | null | undefined): string {
  if (value === null || value === undefined || Number.isNaN(value)) return '—'
  return quantityFormatter.format(value)
}

export function plClassName(value: number | null | undefined): string {
  if (value === null || value === undefined || value === 0) return 'text-secondary'
  return value > 0 ? 'text-success' : 'text-danger'
}

export function plSignedCurrency(value: number | null | undefined): string {
  if (value === null || value === undefined) return '—'
  const formatted = formatCurrency(Math.abs(value))
  if (value > 0) return `+${formatted}`
  if (value < 0) return `−${formatted}`
  return formatted
}

export function plSignedPercent(value: number | null | undefined): string {
  if (value === null || value === undefined) return '—'
  const formatted = formatPercent(Math.abs(value))
  if (value > 0) return `+${formatted}`
  if (value < 0) return `−${formatted}`
  return formatted
}

const bigIntFormatter = new Intl.NumberFormat('en-US', {
  notation: 'compact',
  maximumFractionDigits: 1,
})

export function formatLargeNumber(value: number | null | undefined): string {
  if (value === null || value === undefined || Number.isNaN(value)) return '—'
  return bigIntFormatter.format(value)
}

export function formatLargeCurrency(value: number | null | undefined): string {
  if (value === null || value === undefined || Number.isNaN(value)) return '—'
  return '$' + bigIntFormatter.format(value)
}
