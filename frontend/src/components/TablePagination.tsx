import { Form } from 'react-bootstrap'
import { useTranslation } from 'react-i18next'
import { Button } from './Button'
import { Icon } from './Icon'

const DEFAULT_PAGE_SIZE_OPTIONS = [10, 25, 50, 100]

type Props = {
  page: number
  pageSize: number
  totalCount: number
  onPageChange: (page: number) => void
  onPageSizeChange: (size: number) => void
  pageSizeOptions?: number[]
  hideSizeSelector?: boolean
}

/**
 * Builds the list of page numbers to show, with ellipsis ("…") for gaps.
 * Always shows: first, last, current, current±1 (and the gaps around them).
 *
 * Example outputs (current page in brackets):
 *   total=3  current=2  →  1 [2] 3
 *   total=10 current=1  →  [1] 2 3 … 10
 *   total=10 current=5  →  1 … 4 [5] 6 … 10
 *   total=10 current=10 →  1 … 8 9 [10]
 */
function buildPageList(total: number, current: number): (number | '…')[] {
  if (total <= 7) {
    return Array.from({ length: total }, (_, i) => i + 1)
  }
  const pages: (number | '…')[] = [1]
  const left = Math.max(2, current - 1)
  const right = Math.min(total - 1, current + 1)
  if (left > 2) pages.push('…')
  for (let i = left; i <= right; i++) pages.push(i)
  if (right < total - 1) pages.push('…')
  pages.push(total)
  return pages
}

export function TablePagination({
  page,
  pageSize,
  totalCount,
  onPageChange,
  onPageSizeChange,
  pageSizeOptions = DEFAULT_PAGE_SIZE_OPTIONS,
  hideSizeSelector,
}: Props) {
  const { t } = useTranslation()
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize))
  const safePage = Math.min(Math.max(1, page), totalPages)
  const firstIdx = totalCount === 0 ? 0 : (safePage - 1) * pageSize + 1
  const lastIdx = Math.min(safePage * pageSize, totalCount)

  if (totalCount === 0) return null

  const pageList = buildPageList(totalPages, safePage)

  return (
    <div className="table-pagination">
      {!hideSizeSelector && (
        <div className="d-flex align-items-center gap-2">
          <span className="text-muted text-nowrap">{t('pagination.rowsPerPage')}:</span>
          <Form.Select
            size="sm"
            style={{ width: 'auto', minWidth: 76 }}
            value={pageSize}
            onChange={(e) => onPageSizeChange(Number(e.target.value))}
            aria-label={t('pagination.rowsPerPage')}
          >
            {pageSizeOptions.map((opt) => (
              <option key={opt} value={opt}>{opt}</option>
            ))}
          </Form.Select>
        </div>
      )}

      <div className="text-muted text-nowrap small">
        {t('pagination.range', { from: firstIdx, to: lastIdx, total: totalCount })}
      </div>

      <div className="d-flex gap-1 flex-wrap">
        <Button
          size="sm"
          variant="secondary"
          outline
          disabled={safePage <= 1}
          onClick={() => onPageChange(1)}
          aria-label={t('pagination.first')}
          title={t('pagination.first')}
        >
          <Icon name="rr-angle-double-left" />
        </Button>
        <Button
          size="sm"
          variant="secondary"
          outline
          disabled={safePage <= 1}
          onClick={() => onPageChange(safePage - 1)}
          aria-label={t('pagination.previous')}
          title={t('pagination.previous')}
        >
          <Icon name="rr-angle-left" />
        </Button>

        {pageList.map((p, i) =>
          p === '…' ? (
            <span key={`gap-${i}`} className="px-2 align-self-center text-muted">…</span>
          ) : (
            <Button
              key={p}
              size="sm"
              variant="primary"
              outline={p !== safePage}
              onClick={() => onPageChange(p)}
              aria-current={p === safePage ? 'page' : undefined}
              aria-label={`Page ${p}`}
              className="page-number-btn"
            >
              {p}
            </Button>
          ),
        )}

        <Button
          size="sm"
          variant="secondary"
          outline
          disabled={safePage >= totalPages}
          onClick={() => onPageChange(safePage + 1)}
          aria-label={t('pagination.next')}
          title={t('pagination.next')}
        >
          <Icon name="rr-angle-right" />
        </Button>
        <Button
          size="sm"
          variant="secondary"
          outline
          disabled={safePage >= totalPages}
          onClick={() => onPageChange(totalPages)}
          aria-label={t('pagination.last')}
          title={t('pagination.last')}
        >
          <Icon name="rr-angle-double-right" />
        </Button>
      </div>
    </div>
  )
}
