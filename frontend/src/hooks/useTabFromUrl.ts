import { useCallback } from 'react'
import { useSearchParams } from 'react-router-dom'

/**
 * Bind a tab key to the URL `?key=value` search param.
 *
 * - Reading: returns the current param value, or `defaultValue` if absent.
 * - Writing: replaces the entry in history (doesn't push) so the back button
 *   exits the page instead of cycling tabs.
 * - When the new value equals the default, the param is removed (clean URLs).
 */
export function useTabFromUrl(key: string, defaultValue: string): [string, (next: string) => void] {
  const [params, setParams] = useSearchParams()
  const current = params.get(key) ?? defaultValue

  const setTab = useCallback(
    (next: string) => {
      setParams(
        (prev) => {
          const np = new URLSearchParams(prev)
          if (!next || next === defaultValue) np.delete(key)
          else np.set(key, next)
          return np
        },
        { replace: true },
      )
    },
    [key, defaultValue, setParams],
  )

  return [current, setTab]
}
