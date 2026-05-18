import { Form } from 'react-bootstrap'
import { useFormContext, type FieldError } from 'react-hook-form'
import { useTranslation } from 'react-i18next'

type Props = {
  name: string
  label: string
  type?: 'text' | 'email' | 'tel' | 'url' | 'search'
  placeholder?: string
  autoComplete?: string
  autoFocus?: boolean
  disabled?: boolean
  helpText?: string
}

export function TextField({
  name,
  label,
  type = 'text',
  placeholder,
  autoComplete,
  autoFocus,
  disabled,
  helpText,
}: Props) {
  const { t } = useTranslation()
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
      <Form.Control
        type={type}
        placeholder={placeholder}
        autoComplete={autoComplete}
        autoFocus={autoFocus}
        disabled={disabled}
        isInvalid={Boolean(fieldError)}
        {...register(name)}
      />
      {fieldError ? (
        <Form.Control.Feedback type="invalid">{errorMessage}</Form.Control.Feedback>
      ) : helpText ? (
        <Form.Text className="text-muted">{helpText}</Form.Text>
      ) : null}
    </Form.Group>
  )
}
