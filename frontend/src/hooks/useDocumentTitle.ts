import { useEffect } from 'react'

const APP_NAME = 'Trading313'

/**
 * Sets `document.title` to `${title} · Trading313` while the component is
 * mounted. Pass an empty/undefined title to fall back to just the app name.
 * Restores the previous title on unmount so SPA navigation doesn't leave a
 * stale tab title behind.
 */
export function useDocumentTitle(title?: string | null) {
  useEffect(() => {
    const prev = document.title
    document.title = title ? `${title} · ${APP_NAME}` : APP_NAME
    return () => {
      document.title = prev
    }
  }, [title])
}
