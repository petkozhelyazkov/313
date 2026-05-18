import { useEffect } from 'react'
import { useLocation } from 'react-router-dom'

/**
 * Watches `location.hash` and scrolls the matching `id` into view whenever
 * it changes. React Router v6 doesn't do this by default — without it,
 * clicking a link like `/profile#alerts` just navigates without scrolling.
 *
 * Falls back to scrolling to top if the hash points at an id that doesn't
 * exist (yet). Retries once after a microtask in case the target is rendered
 * after the route mount (lazy queries, async data, etc.).
 */
export function ScrollToHash() {
  const { hash, pathname } = useLocation()

  useEffect(() => {
    if (!hash) {
      window.scrollTo({ top: 0, behavior: 'auto' })
      return
    }

    const id = hash.replace(/^#/, '')
    if (!id) return

    const scroll = () => {
      const el = document.getElementById(id)
      if (el) {
        el.scrollIntoView({ behavior: 'smooth', block: 'start' })
        return true
      }
      return false
    }

    // Try immediately, then after the next paint to catch async-rendered targets.
    if (!scroll()) {
      const t = window.setTimeout(scroll, 120)
      return () => window.clearTimeout(t)
    }
  }, [hash, pathname])

  return null
}
