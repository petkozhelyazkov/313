import { Container } from 'react-bootstrap'
import { useTranslation } from 'react-i18next'
import { SystemPanel } from '../components/admin/SystemPanel'
import { UsersTable } from '../components/admin/UsersTable'
import { useDocumentTitle } from '../hooks/useDocumentTitle'

export function AdminPage() {
  const { t } = useTranslation()
  useDocumentTitle(t('admin.title'))
  return (
    <Container className="py-4">
      <h1 className="h3 mb-3">{t('admin.title')}</h1>
      <SystemPanel />
      <h2 className="h5 mb-2">{t('admin.users')}</h2>
      <UsersTable />
    </Container>
  )
}
