import { useState } from 'react'
import { Container, Card, ListGroup, Spinner } from 'react-bootstrap'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'
import { useDigests, useDigest, useGenerateDigest } from '../api/digests'
import { useDocumentTitle } from '../hooks/useDocumentTitle'
import { Button } from '../components/Button'
import { toast } from 'react-hot-toast'
import { getApiErrorMessage } from '../api/client'

export function DigestsPage() {
  const { t, i18n } = useTranslation()
  useDocumentTitle(t('digests.title'))
  const { data: list, isLoading } = useDigests()
  const [selected, setSelected] = useState<number | null>(null)
  const { data: detail } = useDigest(selected)
  const generate = useGenerateDigest()

  const fmt = (s: string) =>
    new Date(s).toLocaleString(i18n.language, { dateStyle: 'medium', timeStyle: 'short' })

  return (
    <Container className="py-4">
      <div className="d-flex justify-content-between align-items-center flex-wrap gap-2 mb-3">
        <h1 className="h3 mb-0">{t('digests.title')}</h1>
        <div className="d-flex gap-2">
          <Button
            variant="primary"
            outline
            onClick={async () => {
              try {
                await generate.mutateAsync()
                toast.success(t('digests.generated'))
              } catch (err) {
                toast.error(getApiErrorMessage(err))
              }
            }}
            loading={generate.isPending}
          >
            {t('digests.generateNow')}
          </Button>
          <Link to="/profile" className="btn btn-link btn-sm">{t('common.back')}</Link>
        </div>
      </div>

      <div className="row g-3">
        <div className="col-lg-4">
          {isLoading ? (
            <Card body className="text-center"><Spinner size="sm" /></Card>
          ) : !list || list.length === 0 ? (
            <Card body className="text-muted text-center">{t('digests.empty')}</Card>
          ) : (
            <Card>
              <ListGroup variant="flush">
                {list.map((d) => (
                  <ListGroup.Item
                    key={d.id}
                    action
                    active={selected === d.id}
                    onClick={() => setSelected(d.id)}
                    className="d-flex justify-content-between align-items-start"
                  >
                    <div>
                      <div className={d.read ? '' : 'fw-semibold'}>{d.subject}</div>
                      <small className={selected === d.id ? 'text-white-50' : 'text-muted'}>
                        {fmt(d.generatedAt)}
                      </small>
                    </div>
                    {!d.read && <span className="badge bg-primary rounded-pill">{t('digests.new')}</span>}
                  </ListGroup.Item>
                ))}
              </ListGroup>
            </Card>
          )}
        </div>
        <div className="col-lg-8">
          {detail ? (
            <Card>
              <Card.Header>
                <strong>{detail.subject}</strong>
                <div className="text-muted small">
                  {fmt(detail.periodStart)} → {fmt(detail.periodEnd)}
                </div>
              </Card.Header>
              <Card.Body>
                <div dangerouslySetInnerHTML={{ __html: detail.bodyHtml }} />
              </Card.Body>
            </Card>
          ) : (
            <Card body className="text-muted text-center py-5">{t('digests.selectHint')}</Card>
          )}
        </div>
      </div>
    </Container>
  )
}
