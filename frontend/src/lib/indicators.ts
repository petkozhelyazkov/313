// Pure-math technical indicator helpers. Compute locally from history points
// so we don't burn Twelve Data quota on overlay toggling.

export type ClosePoint = { date: string; close: number }
export type IndicatorPoint = { date: string; value: number | null }
export type BollingerPoint = { date: string; upper: number | null; middle: number | null; lower: number | null }

export function sma(points: ClosePoint[], period: number): IndicatorPoint[] {
  if (period <= 0) return points.map((p) => ({ date: p.date, value: null }))
  const result: IndicatorPoint[] = []
  let sum = 0
  for (let i = 0; i < points.length; i++) {
    sum += points[i].close
    if (i >= period) sum -= points[i - period].close
    result.push({
      date: points[i].date,
      value: i >= period - 1 ? sum / period : null,
    })
  }
  return result
}

export function ema(points: ClosePoint[], period: number): IndicatorPoint[] {
  if (period <= 0) return points.map((p) => ({ date: p.date, value: null }))
  const k = 2 / (period + 1)
  const result: IndicatorPoint[] = []
  let prev: number | null = null
  let seedSum = 0
  for (let i = 0; i < points.length; i++) {
    if (i < period) {
      seedSum += points[i].close
      if (i === period - 1) prev = seedSum / period
      const value: number | null = i === period - 1 ? prev : null
      result.push({ date: points[i].date, value })
    } else {
      const computed: number = points[i].close * k + (prev ?? 0) * (1 - k)
      result.push({ date: points[i].date, value: computed })
      prev = computed
    }
  }
  return result
}

export function rsi(points: ClosePoint[], period: number = 14): IndicatorPoint[] {
  if (period <= 0) return points.map((p) => ({ date: p.date, value: null }))
  const result: IndicatorPoint[] = []
  let avgGain = 0
  let avgLoss = 0
  for (let i = 0; i < points.length; i++) {
    if (i === 0) {
      result.push({ date: points[i].date, value: null })
      continue
    }
    const delta = points[i].close - points[i - 1].close
    const gain = Math.max(delta, 0)
    const loss = Math.max(-delta, 0)
    if (i < period) {
      avgGain += gain
      avgLoss += loss
      if (i === period - 1) {
        avgGain /= period
        avgLoss /= period
        const rs = avgLoss === 0 ? Infinity : avgGain / avgLoss
        const value = 100 - 100 / (1 + rs)
        result.push({ date: points[i].date, value })
      } else {
        result.push({ date: points[i].date, value: null })
      }
    } else {
      avgGain = (avgGain * (period - 1) + gain) / period
      avgLoss = (avgLoss * (period - 1) + loss) / period
      const rs = avgLoss === 0 ? Infinity : avgGain / avgLoss
      const value = 100 - 100 / (1 + rs)
      result.push({ date: points[i].date, value })
    }
  }
  return result
}

export function bollingerBands(
  points: ClosePoint[],
  period: number = 20,
  stdDevMultiplier: number = 2,
): BollingerPoint[] {
  const smaSeries = sma(points, period)
  const result: BollingerPoint[] = []
  for (let i = 0; i < points.length; i++) {
    if (i < period - 1) {
      result.push({ date: points[i].date, upper: null, middle: null, lower: null })
      continue
    }
    const window = points.slice(i - period + 1, i + 1).map((p) => p.close)
    const mean = smaSeries[i].value ?? 0
    const variance = window.reduce((acc, v) => acc + (v - mean) ** 2, 0) / period
    const stdDev = Math.sqrt(variance)
    result.push({
      date: points[i].date,
      middle: mean,
      upper: mean + stdDevMultiplier * stdDev,
      lower: mean - stdDevMultiplier * stdDev,
    })
  }
  return result
}
