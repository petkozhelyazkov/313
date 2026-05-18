import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useMutation } from '@tanstack/react-query'
import { Container, Card, Row, Col, Modal, Form, Table } from 'react-bootstrap'
import { useTranslation } from 'react-i18next'
import { Button, ButtonGroup } from '../components/Button'
import { FormProvider, useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { TextField, PasswordField, FormSubmitButton } from '../components/forms'
import { useAuth } from '../auth/useAuth'
import { updateProfile, changePassword } from '../api/users'
import { useAdjustCash, useCashHistory } from '../api/cash'
import { AlertsList } from '../components/AlertsList'
import { AchievementsCard } from '../components/AchievementsCard'
import { RecurringOrdersCard } from '../components/RecurringOrdersCard'
import { GoalsCard } from '../components/GoalsCard'
import { PreferencesCard } from '../components/PreferencesCard'
import { TablePagination } from '../components/TablePagination'
import { usePagedData } from '../hooks/usePagedData'
import { useDocumentTitle } from '../hooks/useDocumentTitle'
import { Icon } from '../components/Icon'
import { toast } from '../lib/toast'
import { getApiErrorMessage, isApiError } from '../api/client'
import { downloadFile } from '../lib/download'
import { formatCurrency } from '../lib/format'

const profileSchema = z.object({
  displayName: z.string().min(1, 'errors.displayNameRequired').max(100, 'errors.displayNameMax'),
})
type ProfileValues = z.infer<typeof profileSchema>

const passwordSchema = z
  .object({
    currentPassword: z.string().min(1, 'errors.currentPasswordRequired'),
    newPassword: z
      .string()
      .min(8, 'errors.passwordMin')
      .regex(/[A-Z]/, 'errors.passwordUpper')
      .regex(/\d/, 'errors.passwordDigit'),
    confirmPassword: z.string().min(1, 'errors.confirmPasswordRequired'),
  })
  .refine((v) => v.newPassword === v.confirmPassword, {
    path: ['confirmPassword'],
    message: 'errors.passwordsMustMatch',
  })
type PasswordValues = z.infer<typeof passwordSchema>

export function ProfilePage() {
  const { t } = useTranslation()
  useDocumentTitle(t('profile.title'))
  const { user, refreshUser } = useAuth()
  const [cashModal, setCashModal] = useState<'Deposit' | 'Withdraw' | null>(null)

  const handleCsvDownload = async (path: string, filename: string) => {
    try {
      await downloadFile(path, filename)
    } catch (err) {
      toast.error(getApiErrorMessage(err))
    }
  }

  const profileForm = useForm<ProfileValues>({
    resolver: zodResolver(profileSchema),
    defaultValues: { displayName: user?.displayName ?? '' },
  })

  const updateMutation = useMutation({
    mutationFn: async (values: ProfileValues) => {
      await updateProfile(values.displayName)
    },
    onSuccess: async () => {
      toast.success(t('toast.profileUpdated'))
      await refreshUser()
    },
    onError: (err) => toast.error(getApiErrorMessage(err)),
  })

  const passwordForm = useForm<PasswordValues>({
    resolver: zodResolver(passwordSchema),
    defaultValues: { currentPassword: '', newPassword: '', confirmPassword: '' },
  })

  const passwordMutation = useMutation({
    mutationFn: async (values: PasswordValues) => {
      await changePassword({
        currentPassword: values.currentPassword,
        newPassword: values.newPassword,
      })
    },
    onSuccess: () => {
      toast.success(t('toast.passwordChanged'))
      passwordForm.reset()
    },
    onError: (err) => {
      if (isApiError(err)) {
        const errors = (err.response?.data as { errors?: Record<string, string[]> } | undefined)?.errors
        if (errors?.['PasswordMismatch']) {
          passwordForm.setError('currentPassword', { message: 'profile.passwordMismatch' })
          return
        }
      }
      toast.error(getApiErrorMessage(err))
    },
  })

  return (
    <Container className="py-4">
      <h1 className="h3 mb-3">{t('profile.title')}</h1>

      <Card className="mb-3">
        <Card.Header><strong>{t('profile.account')}</strong></Card.Header>
        <Card.Body>
          <Row>
            <Col md={6}>
              <div className="text-muted small text-uppercase">{t('profile.email')}</div>
              <div className="mb-3">{user?.email}</div>
            </Col>
            <Col md={6}>
              <div className="text-muted small text-uppercase">{t('profile.roles')}</div>
              <div className="mb-3">{user?.roles.join(', ')}</div>
            </Col>
            <Col md={6}>
              <div className="text-muted small text-uppercase">{t('profile.cashBalance')}</div>
              <div className="d-flex align-items-center gap-2 mb-3">
                <strong>{formatCurrency(user?.cashBalance ?? 0)}</strong>
                <ButtonGroup size="sm">
                  <Button variant="success" outline size="sm" iconLeft={<Icon name="rr-plus" />} onClick={() => setCashModal('Deposit')}>
                    {t('profile.deposit')}
                  </Button>
                  <Button variant="danger" outline size="sm" iconLeft={<Icon name="rr-minus" />} onClick={() => setCashModal('Withdraw')}>
                    {t('profile.withdraw')}
                  </Button>
                </ButtonGroup>
              </div>
            </Col>
            <Col md={6}>
              <div className="text-muted small text-uppercase">{t('profile.memberSince')}</div>
              <div className="mb-3">{user ? new Date(user.createdAt).toLocaleDateString() : '—'}</div>
            </Col>
          </Row>
        </Card.Body>
      </Card>

      <Row className="g-3">
        <Col md={6}>
          <Card>
            <Card.Header><strong>{t('profile.displayNameSection')}</strong></Card.Header>
            <Card.Body>
              <FormProvider {...profileForm}>
                <form onSubmit={profileForm.handleSubmit((v) => updateMutation.mutate(v))} noValidate>
                  <TextField name="displayName" label={t('profile.displayNameLabel')} autoComplete="name" />
                  <FormSubmitButton>{t('common.update')}</FormSubmitButton>
                </form>
              </FormProvider>
            </Card.Body>
          </Card>
        </Col>

        <Col md={6}>
          <Card>
            <Card.Header><strong>{t('profile.changePassword')}</strong></Card.Header>
            <Card.Body>
              <FormProvider {...passwordForm}>
                <form onSubmit={passwordForm.handleSubmit((v) => passwordMutation.mutate(v))} noValidate>
                  <PasswordField name="currentPassword" label={t('profile.currentPassword')} autoComplete="current-password" />
                  <PasswordField name="newPassword" label={t('profile.newPassword')} autoComplete="new-password" />
                  <PasswordField name="confirmPassword" label={t('profile.confirmNewPassword')} autoComplete="new-password" />
                  <FormSubmitButton variant="warning">{t('profile.changePasswordBtn')}</FormSubmitButton>
                </form>
              </FormProvider>
            </Card.Body>
          </Card>
        </Col>

        <Col xs={12}>
          <Card>
            <Card.Header><strong>{t('profile.dataExport')}</strong></Card.Header>
            <Card.Body>
              <p className="text-muted small mb-3">{t('profile.dataExportHint')}</p>
              <div className="d-flex flex-wrap gap-2">
                <Button
                  variant="primary"
                  outline
                  size="sm"
                  iconLeft={<Icon name="rr-download" />}
                  onClick={() => handleCsvDownload('/api/portfolio/transactions.csv', 'transactions.csv')}
                >
                  {t('profile.transactionsCsv')}
                </Button>
                <Button
                  variant="primary"
                  outline
                  size="sm"
                  iconLeft={<Icon name="rr-download" />}
                  onClick={() => handleCsvDownload('/api/portfolio/positions.csv', 'positions.csv')}
                >
                  {t('profile.positionsCsv')}
                </Button>
                <Link to="/tax-report" className="btn btn-x btn-x-primary btn-x-outline btn-x-sm">
                  <span className="btn-x-icon"><Icon name="rr-file-invoice-dollar" /></span>
                  <span className="btn-x-label">{t('tax.title')}</span>
                </Link>
              </div>
              <p className="text-muted small mt-2 mb-0">{t('profile.csvNote')}</p>
            </Card.Body>
          </Card>
        </Col>

        <Col xs={12}>
          <PreferencesCard />
        </Col>

        <Col xs={12}>
          <GoalsCard />
        </Col>

        <Col xs={12}>
          <AchievementsCard />
        </Col>

        <Col xs={12}>
          <RecurringOrdersCard />
        </Col>

        <Col xs={12}>
          <CashHistorySection />
        </Col>

        <Col xs={12}>
          <AlertsList />
        </Col>
      </Row>

      <CashModal mode={cashModal} onClose={() => setCashModal(null)} />
    </Container>
  )
}

function CashHistorySection() {
  const { t } = useTranslation()
  const { data, isLoading } = useCashHistory()
  const paged = usePagedData(data ?? [], { defaultPageSize: 10, storageKey: 'cashHistory' })
  if (isLoading) return null
  if (!data || data.length === 0) return null
  return (
    <Card>
      <Card.Header><strong>{t('profile.recentCash')}</strong></Card.Header>
      <Card.Body className="p-0">
        <Table responsive className="mb-0 small align-middle">
          <thead>
            <tr>
              <th>{t('profile.when')}</th>
              <th>{t('portfolio.type')}</th>
              <th className="text-end">{t('profile.amount')}</th>
              <th className="text-end">{t('profile.balanceAfter')}</th>
              <th>{t('watchlist.notes')}</th>
            </tr>
          </thead>
          <tbody>
            {paged.items.map((c) => (
              <tr key={c.id}>
                <td className="text-muted">{new Date(c.executedAt).toLocaleString()}</td>
                <td>
                  <span className={`badge bg-${c.type === 'Deposit' ? 'success' : 'danger'}`}>
                    {t(c.type === 'Deposit' ? 'profile.deposit' : 'profile.withdraw')}
                  </span>
                </td>
                <td className={`text-end fw-semibold ${c.type === 'Deposit' ? 'text-success' : 'text-danger'}`}>
                  {c.type === 'Deposit' ? '+' : '−'}{formatCurrency(c.amount)}
                </td>
                <td className="text-end">{formatCurrency(c.balanceAfter)}</td>
                <td className="text-muted">{c.notes || '—'}</td>
              </tr>
            ))}
          </tbody>
        </Table>
        <TablePagination
          page={paged.page}
          pageSize={paged.pageSize}
          totalCount={paged.total}
          onPageChange={paged.setPage}
          onPageSizeChange={paged.setPageSize}
        />
      </Card.Body>
    </Card>
  )
}

function CashModal({ mode, onClose }: { mode: 'Deposit' | 'Withdraw' | null; onClose: () => void }) {
  const { t } = useTranslation()
  const [amount, setAmount] = useState('')
  const [notes, setNotes] = useState('')
  const { user, refreshUser } = useAuth()
  const adjust = useAdjustCash()
  const isOpen = mode !== null
  const amtNum = Number(amount)
  const isValid = amount !== '' && Number.isFinite(amtNum) && amtNum > 0

  const submit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!mode || !isValid) return
    try {
      await adjust.mutateAsync({ type: mode, amount: amtNum, notes: notes || undefined })
      toast.success(t(mode === 'Deposit' ? 'toast.deposited' : 'toast.withdrew', { amount: formatCurrency(amtNum) }))
      await refreshUser()
      onClose()
      setAmount('')
      setNotes('')
    } catch (err) {
      toast.error(getApiErrorMessage(err))
    }
  }

  return (
    <Modal show={isOpen} onHide={onClose} centered>
      <Modal.Header closeButton>
        <Modal.Title>{t(mode === 'Deposit' ? 'profile.depositCash' : 'profile.withdrawCash')}</Modal.Title>
      </Modal.Header>
      <Form onSubmit={submit}>
        <Modal.Body>
          <div className="d-flex justify-content-between mb-3">
            <span className="text-muted">{t('profile.currentBalance')}</span>
            <strong>{formatCurrency(user?.cashBalance ?? 0)}</strong>
          </div>
          <Form.Group className="mb-3" controlId="cash-amount">
            <Form.Label>{t('profile.amount')}</Form.Label>
            <Form.Control
              type="number"
              step="any"
              min={0}
              value={amount}
              onChange={(e) => setAmount(e.target.value)}
              autoFocus
            />
          </Form.Group>
          <Form.Group className="mb-3" controlId="cash-notes">
            <Form.Label>{t('profile.notesOptional')}</Form.Label>
            <Form.Control as="textarea" rows={2} value={notes} onChange={(e) => setNotes(e.target.value)} />
          </Form.Group>
          {mode === 'Withdraw' && isValid && amtNum > (user?.cashBalance ?? 0) && (
            <div className="text-danger small">
              {t('profile.insufficientFunds', { amount: formatCurrency(user?.cashBalance ?? 0) })}
            </div>
          )}
        </Modal.Body>
        <Modal.Footer>
          <Button variant="secondary" outline onClick={onClose} disabled={adjust.isPending}>{t('common.cancel')}</Button>
          <Button
            type="submit"
            variant={mode === 'Deposit' ? 'success' : 'danger'}
            disabled={!isValid}
            loading={adjust.isPending}
          >
            {t(mode === 'Deposit' ? 'profile.confirmDeposit' : 'profile.confirmWithdraw')}
          </Button>
        </Modal.Footer>
      </Form>
    </Modal>
  )
}
