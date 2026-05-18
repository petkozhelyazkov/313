import { Container } from 'react-bootstrap'
import { Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { useDocumentTitle } from '../hooks/useDocumentTitle'

export function NotFoundPage() {
  const { t } = useTranslation()
  useDocumentTitle('404')
  return (
    <Container className="py-5 text-center">
      <h1 className="display-5">404</h1>
      <p className="lead">{t('notFound.message', { defaultValue: 'We couldn’t find that page.' })}</p>
      <Link to="/">{t('notFound.backHome', { defaultValue: 'Back to dashboard' })}</Link>
    </Container>
  )
}
