import { useState } from 'react'
import { Form, InputGroup } from 'react-bootstrap'
import { useFormContext, type FieldError } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { Button } from '../Button'

type Props = {
  name: string
  label: string
  placeholder?: string
  autoComplete?: 'current-password' | 'new-password'
  autoFocus?: boolean
  disabled?: boolean
}

export function PasswordField({
  name,
  label,
  placeholder,
  autoComplete = 'current-password',
  autoFocus,
  disabled,
}: Props) {
  const { t } = useTranslation()
  const [visible, setVisible] = useState(false)
  const {
    register,
    formState: { errors },
  } = useFormContext()
  const fieldError = errors[name] as FieldError | undefined
  const errorMessage = fieldError?.message
    ? t(String(fieldError.message), { defaultValue: String(fieldError.message) })
    : 'Invalid value'

  return (
    <Form.Group className="mb-3" controlId={name}>
      <Form.Label>{label}</Form.Label>
      <InputGroup hasValidation>
        <Form.Control
          type={visible ? 'text' : 'password'}
          placeholder={placeholder}
          autoComplete={autoComplete}
          autoFocus={autoFocus}
          disabled={disabled}
          isInvalid={Boolean(fieldError)}
          {...register(name)}
        />
        <Button
          type="button"
          variant="secondary"
          outline
          size="sm"
          className="rounded-start-0"
          onClick={() => setVisible((v) => !v)}
          aria-label={visible ? t('auth.hide') : t('auth.show')}
          disabled={disabled}
        >
          {visible ? t('auth.hide') : t('auth.show')}
        </Button>
        {fieldError && (
          <Form.Control.Feedback type="invalid">{errorMessage}</Form.Control.Feedback>
        )}
      </InputGroup>
    </Form.Group>
  )
}
