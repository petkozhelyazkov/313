import { useEffect, useState } from 'react'
import { Modal, Form } from 'react-bootstrap'
import { useTranslation } from 'react-i18next'
import { toast } from 'react-hot-toast'
import { Button } from '../Button'
import { useUpdatePositionMutation, tradeErrorMessage } from '../../api/portfolio'
import type { PositionDto } from '../../api/portfolio'

type Props = {
  position: PositionDto | null
  onClose: () => void
}

export function EditPositionModal({ position, onClose }: Props) {
  const { t } = useTranslation()
  const update = useUpdatePositionMutation()
  const [notes, setNotes] = useState('')
  const [tags, setTags] = useState('')

  useEffect(() => {
    if (position) {
      setNotes(position.notes ?? '')
      setTags(position.tags ?? '')
    }
  }, [position])

  const handleSave = async () => {
    if (!position) return
    try {
      await update.mutateAsync({
        symbol: position.symbol,
        notes: notes.trim() === '' ? null : notes,
        tags: tags.trim() === '' ? null : tags,
      })
      toast.success(t('portfolio.positionUpdated'))
      onClose()
    } catch (err) {
      toast.error(tradeErrorMessage(err))
    }
  }

  return (
    <Modal show={position !== null} onHide={onClose} centered>
      <Modal.Header closeButton>
        <Modal.Title>
          {t('portfolio.editPosition')} {position && <small className="text-muted">· {position.symbol}</small>}
        </Modal.Title>
      </Modal.Header>
      <Modal.Body>
        <Form>
          <Form.Group className="mb-3">
            <Form.Label>{t('portfolio.notes')}</Form.Label>
            <Form.Control
              as="textarea"
              rows={4}
              maxLength={1000}
              value={notes}
              onChange={(e) => setNotes(e.target.value)}
              placeholder={t('portfolio.notesPlaceholder')}
            />
            <Form.Text className="text-muted">{notes.length}/1000</Form.Text>
          </Form.Group>
          <Form.Group>
            <Form.Label>{t('portfolio.tags')}</Form.Label>
            <Form.Control
              type="text"
              maxLength={200}
              value={tags}
              onChange={(e) => setTags(e.target.value)}
              placeholder={t('portfolio.tagsPlaceholder')}
            />
            <Form.Text className="text-muted">{t('portfolio.tagsHint')}</Form.Text>
          </Form.Group>
        </Form>
      </Modal.Body>
      <Modal.Footer>
        <Button variant="secondary" outline onClick={onClose} disabled={update.isPending}>
          {t('common.cancel')}
        </Button>
        <Button variant="primary" onClick={handleSave} loading={update.isPending}>
          {t('common.save')}
        </Button>
      </Modal.Footer>
    </Modal>
  )
}
