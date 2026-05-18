import { useState } from 'react'
import { Table, Form, Badge, Modal, Card } from 'react-bootstrap'
import { useTranslation } from 'react-i18next'
import { Button } from '../Button'
import { TablePagination } from '../TablePagination'
import { usePersistedNumber } from '../../hooks/usePersistedNumber'
import { useAdminUsers, useSetRole, useSetActive, type AdminUserDto } from '../../api/admin'
import { formatCurrency } from '../../lib/format'
import { toast } from '../../lib/toast'
import { getApiErrorMessage } from '../../api/client'

type ConfirmState = {
  target: AdminUserDto
  action: 'toggle-role' | 'toggle-active'
  nextRole?: 'User' | 'Admin'
  nextActive?: boolean
  title: string
  body: string
} | null

export function UsersTable() {
  const { t } = useTranslation()
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = usePersistedNumber('adminUsers', 20)
  const [filter, setFilter] = useState('')
  const [active, setActive] = useState<string | undefined>(undefined)
  const [confirm, setConfirm] = useState<ConfirmState>(null)

  const { data, isLoading, isError } = useAdminUsers(page, pageSize, active)
  const setRole = useSetRole()
  const setActiveFlag = useSetActive()

  const apply = (e: React.FormEvent) => {
    e.preventDefault()
    setPage(1)
    setActive(filter.trim() || undefined)
  }

  const confirmRoleToggle = (u: AdminUserDto) => {
    const isAdmin = u.roles.includes('Admin')
    const next = isAdmin ? 'User' : 'Admin'
    setConfirm({
      target: u,
      action: 'toggle-role',
      nextRole: next,
      title: t(isAdmin ? 'admin.demoteTitle' : 'admin.promoteTitle'),
      body: t(isAdmin ? 'admin.demoteBody' : 'admin.promoteBody', { email: u.email }),
    })
  }

  const confirmActiveToggle = (u: AdminUserDto) => {
    setConfirm({
      target: u,
      action: 'toggle-active',
      nextActive: !u.isActive,
      title: t(u.isActive ? 'admin.disableTitle' : 'admin.enableTitle'),
      body: t(u.isActive ? 'admin.disableBody' : 'admin.enableBody', { email: u.email }),
    })
  }

  const runConfirmedAction = async () => {
    if (!confirm) return
    try {
      if (confirm.action === 'toggle-role' && confirm.nextRole) {
        await setRole.mutateAsync({ id: confirm.target.id, role: confirm.nextRole })
        toast.success(t('admin.roleChanged', { email: confirm.target.email, role: confirm.nextRole }))
      } else if (confirm.action === 'toggle-active' && confirm.nextActive !== undefined) {
        await setActiveFlag.mutateAsync({ id: confirm.target.id, isActive: confirm.nextActive })
        toast.success(t(confirm.nextActive ? 'admin.userEnabled' : 'admin.userDisabled', { email: confirm.target.email }))
      }
    } catch (err) {
      toast.error(getApiErrorMessage(err))
    } finally {
      setConfirm(null)
    }
  }

  return (
    <>
      <Form className="d-flex gap-2 mb-3 align-items-end" onSubmit={apply}>
        <Form.Group>
          <Form.Label className="small mb-1">{t('admin.filterPlaceholder')}</Form.Label>
          <Form.Control
            size="sm"
            placeholder={t('admin.filterExample')}
            value={filter}
            onChange={(e) => setFilter(e.target.value)}
            style={{ width: 240 }}
          />
        </Form.Group>
        <Button size="sm" variant="primary" type="submit">{t('common.apply')}</Button>
        {active && (
          <Button
            size="sm"
            variant="ghost"
            type="button"
            onClick={() => {
              setFilter('')
              setActive(undefined)
              setPage(1)
            }}
          >
            {t('common.clear')}
          </Button>
        )}
      </Form>

      {isError ? (
        <Card body className="text-center text-muted">{t('admin.couldNotLoadUsers')}</Card>
      ) : isLoading || !data ? (
        <Card body className="text-center text-muted">{t('common.loading')}</Card>
      ) : data.items.length === 0 ? (
        <Card body className="text-center text-muted">{t('admin.noUsersMatch')}</Card>
      ) : (
        <>
          <Table responsive hover className="align-middle">
            <thead className="table-light sticky-top">
              <tr>
                <th>{t('admin.email')}</th>
                <th>{t('admin.displayName')}</th>
                <th>{t('admin.roles')}</th>
                <th className="text-end">{t('admin.cash')}</th>
                <th>{t('admin.active')}</th>
                <th>{t('admin.created')}</th>
                <th />
              </tr>
            </thead>
            <tbody>
              {data.items.map((u) => (
                <tr key={u.id}>
                  <td className="small">{u.email}</td>
                  <td>{u.displayName}</td>
                  <td>
                    {u.roles.map((r) => (
                      <Badge key={r} bg={r === 'Admin' ? 'primary' : 'secondary'} className="me-1">
                        {r}
                      </Badge>
                    ))}
                  </td>
                  <td className="text-end">{formatCurrency(u.cashBalance)}</td>
                  <td>
                    {u.isActive ? (
                      <Badge bg="success">{t('admin.activeBadge')}</Badge>
                    ) : (
                      <Badge bg="danger">{t('admin.disabledBadge')}</Badge>
                    )}
                  </td>
                  <td className="small text-muted">{new Date(u.createdAt).toLocaleDateString()}</td>
                  <td className="text-end">
                    <Button
                      size="sm"
                      variant={u.roles.includes('Admin') ? 'secondary' : 'primary'}
                      outline
                      onClick={() => confirmRoleToggle(u)}
                      disabled={setRole.isPending}
                      className="me-1"
                    >
                      {t(u.roles.includes('Admin') ? 'admin.demote' : 'admin.promote')}
                    </Button>
                    <Button
                      size="sm"
                      variant={u.isActive ? 'danger' : 'success'}
                      outline
                      onClick={() => confirmActiveToggle(u)}
                      disabled={setActiveFlag.isPending}
                    >
                      {t(u.isActive ? 'admin.disable' : 'admin.enable')}
                    </Button>
                  </td>
                </tr>
              ))}
            </tbody>
          </Table>

          <TablePagination
            page={page}
            pageSize={pageSize}
            totalCount={data.totalCount}
            onPageChange={setPage}
            onPageSizeChange={(s) => { setPageSize(s); setPage(1) }}
          />
        </>
      )}

      <Modal show={confirm !== null} onHide={() => setConfirm(null)} centered>
        <Modal.Header closeButton>
          <Modal.Title>{confirm?.title}</Modal.Title>
        </Modal.Header>
        <Modal.Body>{confirm?.body}</Modal.Body>
        <Modal.Footer>
          <Button variant="secondary" outline onClick={() => setConfirm(null)}>{t('common.cancel')}</Button>
          <Button variant="primary" onClick={runConfirmedAction}>{t('common.confirm')}</Button>
        </Modal.Footer>
      </Modal>
    </>
  )
}
