import { useEffect, useState } from 'react'
import { Modal, Form } from 'react-bootstrap'
import { useTranslation } from 'react-i18next'
import { toast } from 'react-hot-toast'
import { Button } from '../Button'
import { useUpdateTransactionMutation, tradeErrorMessage } from '../../api/portfolio'
import type { TransactionDto } from '../../api/portfolio'

type Props = {
  transaction: TransactionDto | null
  onClose: () => void
}

export function EditTransactionModal({ transaction, onClose }: Props) {
  const { t } = useTranslation()
  const update = useUpdateTransactionMutation()
  const [notes, setNotes] = useState('')
  const [tags, setTags] = useState('')

  useEffect(() => {
    if (transaction) {
      setNotes(transaction.notes ?? '')
      setTags(transaction.tags ?? '')
    }
  }, [transaction])

  const handleSave = async () => {
    if (!transaction) return
    try {
      await update.mutateAsync({
        id: transaction.id,
        notes: notes.trim() === '' ? null : notes,
        tags: tags.trim() === '' ? null : tags,
      })
      toast.success(t('portfolio.transactionUpdated'))
      onClose()
    } catch (err) {
      toast.error(tradeErrorMessage(err))
    }
  }

  return (
    <Modal show={transaction !== null} onHide={onClose} centered>
      <Modal.Header closeButton>
        <Modal.Title>
          {t('portfolio.editTransaction')}
          {transaction && (
            <small className="text-muted ms-2">
              · {transaction.type} {transaction.quantity} {transaction.symbol}
            </small>
          )}
        </Modal.Title>
      </Modal.Header>
      <Modal.Body>
        <Form>
          <Form.Group className="mb-3">
            <Form.Label>{t('portfolio.notes')}</Form.Label>
            <Form.Control
              as="textarea"
              rows={3}
              maxLength={500}
              value={notes}
              onChange={(e) => setNotes(e.target.value)}
              placeholder={t('portfolio.txnNotesPlaceholder')}
            />
            <Form.Text className="text-muted">{notes.length}/500</Form.Text>
          </Form.Group>
          <Form.Group>
            <Form.Label>{t('portfolio.tags')}</Form.Label>
            <Form.Control
              type="text"
              maxLength={200}
              value={tags}
              onChange={(e) => setTags(e.target.value)}
              placeholder={t('portfolio.txnTagsPlaceholder')}
            />
            <Form.Text className="text-muted">{t('portfolio.txnTagsHint')}</Form.Text>
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
