import { useCallback, useState } from 'react'

const STORAGE_PREFIX = 'trading212.pageSize.'

/**
 * Number state that persists to localStorage under a stable key.
 * Reads the value lazily on first render; writes whenever the setter is called.
 * Pass an empty/undefined `storageKey` to disable persistence (acts like useState).
 */
export function usePersistedNumber(
  storageKey: string | undefined | null,
  defaultValue: number,
) {
  const [value, setValueState] = useState<number>(() => {
    if (!storageKey) return defaultValue
    try {
      const raw = localStorage.getItem(STORAGE_PREFIX + storageKey)
      const parsed = raw ? Number(raw) : NaN
      return Number.isFinite(parsed) && parsed > 0 ? parsed : defaultValue
    } catch {
      return defaultValue
    }
  })

  const setValue = useCallback((next: number) => {
    setValueState(next)
    if (!storageKey) return
    try {
      localStorage.setItem(STORAGE_PREFIX + storageKey, String(next))
    } catch {
      /* ignore */
    }
  }, [storageKey])

  return [value, setValue] as const
}
