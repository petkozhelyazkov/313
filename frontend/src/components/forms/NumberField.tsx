import { Form } from 'react-bootstrap'
import { useFormContext, type FieldError } from 'react-hook-form'

type Props = {
  name: string
  label: string
  placeholder?: string
  min?: number
  max?: number
  step?: number | 'any'
  autoFocus?: boolean
  disabled?: boolean
  helpText?: string
}

export function NumberField({
  name,
  label,
  placeholder,
  min,
  max,
  step = 'any',
  autoFocus,
  disabled,
  helpText,
}: Props) {
  const {
    register,
    formState: { errors },
  } = useFormContext()
  const fieldError = errors[name] as FieldError | undefined

  return (
    <Form.Group className="mb-3" controlId={name}>
      <Form.Label>{label}</Form.Label>
      <Form.Control
        type="number"
        inputMode="decimal"
        placeholder={placeholder}
        min={min}
        max={max}
        step={step}
        autoFocus={autoFocus}
        disabled={disabled}
        isInvalid={Boolean(fieldError)}
        {...register(name, { valueAsNumber: true })}
      />
      {fieldError ? (
        <Form.Control.Feedback type="invalid">
          {String(fieldError.message ?? 'Invalid number')}
        </Form.Control.Feedback>
      ) : helpText ? (
        <Form.Text className="text-muted">{helpText}</Form.Text>
      ) : null}
    </Form.Group>
  )
}
