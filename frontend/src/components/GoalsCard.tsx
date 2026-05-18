import { useState } from 'react'
import { Card, ProgressBar, Modal, Form, Spinner } from 'react-bootstrap'
import { useTranslation, Trans } from 'react-i18next'
import { Button } from './Button'
import { Icon } from './Icon'
import {
  useGoals,
  useCreateGoal,
  useUpdateGoal,
  useDeleteGoal,
  type GoalType,
  type GoalDto,
} from '../api/goals'
import { useConfirm } from './ConfirmDialog'
import { formatCurrency } from '../lib/format'
import { toast } from '../lib/toast'
import { getApiErrorMessage } from '../api/client'

const TYPES: GoalType[] = ['PortfolioValue', 'TotalReturn', 'DividendIncome']

function variantFor(pct: number, completed: boolean): 'success' | 'info' | 'warning' | 'danger' {
  if (completed) return 'success'
  if (pct >= 100) return 'success'
  if (pct >= 60) return 'info'
  if (pct >= 25) return 'warning'
  return 'danger'
}

export function GoalsCard() {
  const { t } = useTranslation()
  const { data, isLoading } = useGoals()
  const update = useUpdateGoal()
  const del = useDeleteGoal()
  const confirm = useConfirm()
  const [createOpen, setCreateOpen] = useState(false)

  const handleDelete = async (g: GoalDto) => {
    const ok = await confirm({
      title: t('goals.deleteTitle'),
      body: <Trans i18nKey="goals.deleteBody" values={{ title: g.title ?? t(`goals.type.${g.type}`) }} />,
      confirmLabel: t('common.delete'),
      cancelLabel: t('common.cancel'),
      variant: 'danger',
    })
    if (!ok) return
    try {
      await del.mutateAsync(g.id)
      toast.success(t('toast.goalDeleted'))
    } catch (err) {
      toast.error(getApiErrorMessage(err))
    }
  }

  const handleToggle = async (g: GoalDto) => {
    try {
      await update.mutateAsync({ id: g.id, isCompleted: !g.isCompleted })
      toast.success(t(g.isCompleted ? 'toast.goalReopened' : 'toast.goalCompleted'))
    } catch (err) {
      toast.error(getApiErrorMessage(err))
    }
  }

  return (
    <Card>
      <Card.Header className="d-flex justify-content-between align-items-center">
        <strong><Icon name="sr-target" className="me-2 text-primary" /> {t('goals.title')}</strong>
        <Button size="sm" variant="primary" iconLeft={<Icon name="rr-plus" />} onClick={() => setCreateOpen(true)}>
          {t('goals.newGoal')}
        </Button>
      </Card.Header>
      <Card.Body className="p-0">
        {isLoading && (
          <div className="text-center py-3"><Spinner animation="border" size="sm" /></div>
        )}
        {!isLoading && (!data || data.length === 0) && (
          <div className="p-3 text-muted small">{t('goals.empty')}</div>
        )}
        {data && data.length > 0 && (
          <div className="list-group list-group-flush">
            {data.map((g) => {
              const v = variantFor(g.progressPct, g.isCompleted)
              const remaining = Math.max(0, g.targetAmount - g.currentAmount)
              return (
                <div key={g.id} className={`list-group-item ${g.isCompleted ? 'opacity-50' : ''}`}>
                  <div className="d-flex justify-content-between align-items-start gap-3 mb-2 flex-wrap">
                    <div className="flex-grow-1 min-w-0">
                      <div className="d-flex align-items-center gap-2 flex-wrap">
                        <strong>{g.title ?? t(`goals.type.${g.type}`)}</strong>
                        <span className="badge bg-secondary small">{t(`goals.type.${g.type}`)}</span>
                        {g.dueDate && (
                          <span className="text-muted small">
                            <Icon name="rr-calendar" className="me-1" />
                            {new Date(g.dueDate).toLocaleDateString()}
                          </span>
                        )}
                        {g.isCompleted && (
                          <span className="badge bg-success small">
                            <Icon name="rr-check" className="me-1" />{t('goals.completed')}
                          </span>
                        )}
                      </div>
                      <div className="small text-muted mt-1">
                        <strong className={`text-${v}`}>{formatCurrency(g.currentAmount)}</strong>{' / '}
                        {formatCurrency(g.targetAmount)}
                        {!g.isCompleted && remaining > 0 && (
                          <span className="ms-2">· {t('goals.remaining', { amount: formatCurrency(remaining) })}</span>
                        )}
                      </div>
                    </div>
                    <div className="d-flex gap-1 flex-shrink-0">
                      <Button
                        size="sm"
                        variant={g.isCompleted ? 'warning' : 'success'}
                        outline
                        onClick={() => handleToggle(g)}
                        disabled={update.isPending}
                      >
                        {t(g.isCompleted ? 'goals.reopen' : 'goals.markDone')}
                      </Button>
                      <Button
                        size="sm"
                        variant="danger"
                        outline
                        onClick={() => handleDelete(g)}
                        disabled={del.isPending}
                      >
                        ×
                      </Button>
                    </div>
                  </div>
                  <ProgressBar
                    now={Math.min(100, g.progressPct)}
                    variant={v}
                    style={{ height: 8 }}
                    label={g.progressPct >= 15 ? `${g.progressPct.toFixed(0)}%` : undefined}
                  />
                </div>
              )
            })}
          </div>
        )}
      </Card.Body>

      <CreateGoalModal show={createOpen} onClose={() => setCreateOpen(false)} />
    </Card>
  )
}

function CreateGoalModal({ show, onClose }: { show: boolean; onClose: () => void }) {
  const { t } = useTranslation()
  const [type, setType] = useState<GoalType>('PortfolioValue')
  const [target, setTarget] = useState('15000')
  const [title, setTitle] = useState('')
  const [dueDate, setDueDate] = useState('')
  const create = useCreateGoal()

  const targetNum = Number(target)
  const valid = Number.isFinite(targetNum) && targetNum > 0

  const submit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!valid) return
    try {
      await create.mutateAsync({
        type,
        targetAmount: targetNum,
        title: title.trim() || null,
        dueDate: dueDate || null,
      })
      toast.success(t('toast.goalCreated'))
      onClose()
      setType('PortfolioValue')
      setTarget('15000')
      setTitle('')
      setDueDate('')
    } catch (err) {
      toast.error(getApiErrorMessage(err))
    }
  }

  return (
    <Modal show={show} onHide={onClose} centered>
      <Modal.Header closeButton>
        <Modal.Title>{t('goals.newGoalTitle')}</Modal.Title>
      </Modal.Header>
      <Form onSubmit={submit}>
        <Modal.Body>
          <Form.Group className="mb-3">
            <Form.Label>{t('goals.typeLabel')}</Form.Label>
            <Form.Select value={type} onChange={(e) => setType(e.target.value as GoalType)}>
              {TYPES.map((tType) => (
                <option key={tType} value={tType}>{t(`goals.type.${tType}`)}</option>
              ))}
            </Form.Select>
            <Form.Text className="text-muted">{t(`goals.typeHint.${type}`)}</Form.Text>
          </Form.Group>
          <Form.Group className="mb-3">
            <Form.Label>{t('goals.target')}</Form.Label>
            <Form.Control
              type="number"
              step="any"
              min={0}
              value={target}
              onChange={(e) => setTarget(e.target.value)}
              autoFocus
            />
          </Form.Group>
          <Form.Group className="mb-3">
            <Form.Label>{t('goals.titleOptional')}</Form.Label>
            <Form.Control
              maxLength={120}
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              placeholder={t('goals.titlePlaceholder')}
            />
          </Form.Group>
          <Form.Group className="mb-3">
            <Form.Label>{t('goals.dueDateOptional')}</Form.Label>
            <Form.Control
              type="date"
              value={dueDate}
              onChange={(e) => setDueDate(e.target.value)}
            />
          </Form.Group>
        </Modal.Body>
        <Modal.Footer>
          <Button variant="secondary" outline onClick={onClose} disabled={create.isPending}>
            {t('common.cancel')}
          </Button>
          <Button type="submit" variant="primary" disabled={!valid} loading={create.isPending}>
            {t('common.create')}
          </Button>
        </Modal.Footer>
      </Form>
    </Modal>
  )
}
