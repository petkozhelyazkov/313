import { useEffect, useMemo, useState } from 'react'
import { usePersistedNumber } from './usePersistedNumber'

type Options = {
  defaultPageSize?: number
  /** Persist the page size across reloads under this key (within localStorage). */
  storageKey?: string
}

/**
 * Client-side pagination for an array. Returns a stable slice for the current
 * page plus controls. Page snaps back into range if `data` shrinks (e.g. after
 * a delete) so callers don't end up on an empty page.
 */
export function usePagedData<T>(data: T[], options: Options = {}) {
  const { defaultPageSize = 10, storageKey } = options

  // usePersistedNumber acts like plain useState when storageKey is falsy.
  const [pageSize, setPageSizeRaw] = usePersistedNumber(storageKey, defaultPageSize)
  const [page, setPage] = useState(1)

  const setPageSize = (size: number) => {
    setPageSizeRaw(size)
    setPage(1)
  }

  const total = data.length
  const totalPages = Math.max(1, Math.ceil(total / pageSize))

  useEffect(() => {
    if (page > totalPages) setPage(totalPages)
  }, [page, totalPages])

  const items = useMemo(() => {
    const start = (Math.min(page, totalPages) - 1) * pageSize
    return data.slice(start, start + pageSize)
  }, [data, page, pageSize, totalPages])

  return { items, page, pageSize, total, totalPages, setPage, setPageSize }
}
