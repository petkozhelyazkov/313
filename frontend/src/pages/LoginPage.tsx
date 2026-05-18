import { useEffect } from 'react'
import { Link, useLocation, useNavigate } from 'react-router-dom'
import { Card, Container } from 'react-bootstrap'
import { FormProvider, useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { useTranslation } from 'react-i18next'
import { z } from 'zod'
import { TextField, PasswordField, FormSubmitButton } from '../components/forms'
import { login as apiLogin } from '../api/auth'
import { useAuth } from '../auth/useAuth'
import { useDocumentTitle } from '../hooks/useDocumentTitle'
import { toast } from '../lib/toast'
import { getApiErrorMessage } from '../api/client'

const schema = z.object({
  email: z.string().min(1, 'errors.emailRequired').email('errors.emailInvalid'),
  password: z.string().min(1, 'errors.passwordRequired'),
})

type FormValues = z.infer<typeof schema>

export function LoginPage() {
  const { t } = useTranslation()
  useDocumentTitle(t('nav.signIn'))
  const { login, isAuthenticated } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()

  const params = new URLSearchParams(location.search)
  const returnTo = params.get('returnTo') ?? '/'

  useEffect(() => {
    if (isAuthenticated) navigate(returnTo, { replace: true })
  }, [isAuthenticated, navigate, returnTo])

  const methods = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { email: '', password: '' },
  })

  const onSubmit = methods.handleSubmit(async (values) => {
    try {
      const res = await apiLogin(values.email, values.password)
      login(res.accessToken, res.user, res.expiresAt)
      toast.success(t('auth.welcomeBack', { name: res.user.displayName || res.user.email }))
      navigate(returnTo, { replace: true })
    } catch (err) {
      toast.error(getApiErrorMessage(err))
    }
  })

  return (
    <Container className="d-flex justify-content-center align-items-center py-5" style={{ minHeight: '100vh' }}>
      <Card style={{ maxWidth: 420, width: '100%' }} className="shadow-sm">
        <Card.Body className="p-4">
          <h1 className="h4 mb-1">{t('auth.signIn')}</h1>
          <p className="text-muted small mb-4">{t('auth.welcomeIntro')}</p>
          <FormProvider {...methods}>
            <form onSubmit={onSubmit} noValidate>
              <TextField name="email" label={t('auth.email')} type="email" autoComplete="email" autoFocus />
              <PasswordField name="password" label={t('auth.password')} autoComplete="current-password" />
              <FormSubmitButton fullWidth>{t('auth.signIn')}</FormSubmitButton>
            </form>
          </FormProvider>
          <p className="text-center mt-3 mb-0 small">
            {t('auth.noAccount')} <Link to="/register">{t('auth.register')}</Link>
          </p>
        </Card.Body>
      </Card>
    </Container>
  )
}
