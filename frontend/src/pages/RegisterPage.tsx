import { Link, useNavigate } from 'react-router-dom'
import { Card, Container } from 'react-bootstrap'
import { FormProvider, useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { useTranslation } from 'react-i18next'
import { z } from 'zod'
import { TextField, PasswordField, FormSubmitButton } from '../components/forms'
import { register as apiRegister } from '../api/auth'
import { useDocumentTitle } from '../hooks/useDocumentTitle'
import { toast } from '../lib/toast'
import { getApiErrorMessage, isApiError } from '../api/client'

const schema = z
  .object({
    displayName: z
      .string()
      .min(1, 'errors.displayNameRequired')
      .max(100, 'errors.displayNameMax'),
    email: z.string().min(1, 'errors.emailRequired').email('errors.emailInvalid'),
    password: z
      .string()
      .min(8, 'errors.passwordMin')
      .regex(/[A-Z]/, 'errors.passwordUpper')
      .regex(/\d/, 'errors.passwordDigit'),
    confirmPassword: z.string().min(1, 'errors.confirmPasswordRequired'),
  })
  .refine((v) => v.password === v.confirmPassword, {
    path: ['confirmPassword'],
    message: 'errors.passwordsMustMatch',
  })

type FormValues = z.infer<typeof schema>

export function RegisterPage() {
  const { t } = useTranslation()
  useDocumentTitle(t('nav.register'))
  const navigate = useNavigate()

  const methods = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { displayName: '', email: '', password: '', confirmPassword: '' },
  })

  const onSubmit = methods.handleSubmit(async (values) => {
    try {
      await apiRegister({
        email: values.email,
        password: values.password,
        displayName: values.displayName,
      })
      toast.success(t('auth.accountCreated'))
      navigate('/login', { replace: true })
    } catch (err) {
      if (isApiError(err)) {
        const data = err.response?.data as { errors?: Record<string, string[]> } | undefined
        if (data?.errors?.email) {
          methods.setError('email', { message: data.errors.email[0] })
          return
        }
        if (data?.errors?.['DuplicateUserName']) {
          methods.setError('email', { message: t('auth.emailInUse') })
          return
        }
      }
      toast.error(getApiErrorMessage(err))
    }
  })

  return (
    <Container className="d-flex justify-content-center align-items-center py-5" style={{ minHeight: '100vh' }}>
      <Card style={{ maxWidth: 460, width: '100%' }} className="shadow-sm">
        <Card.Body className="p-4">
          <h1 className="h4 mb-1">{t('auth.createAccount')}</h1>
          <p className="text-muted small mb-4">{t('auth.registerIntro')}</p>
          <FormProvider {...methods}>
            <form onSubmit={onSubmit} noValidate>
              <TextField name="displayName" label={t('auth.displayName')} autoComplete="name" autoFocus />
              <TextField name="email" label={t('auth.email')} type="email" autoComplete="email" />
              <PasswordField
                name="password"
                label={t('auth.password')}
                autoComplete="new-password"
                placeholder={t('auth.passwordHint')}
              />
              <PasswordField
                name="confirmPassword"
                label={t('auth.confirmPassword')}
                autoComplete="new-password"
              />
              <FormSubmitButton fullWidth>{t('auth.createAccount')}</FormSubmitButton>
            </form>
          </FormProvider>
          <p className="text-center mt-3 mb-0 small">
            {t('auth.haveAccount')} <Link to="/login">{t('auth.signIn')}</Link>
          </p>
        </Card.Body>
      </Card>
    </Container>
  )
}
