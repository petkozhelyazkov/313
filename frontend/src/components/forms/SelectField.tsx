import { Form } from 'react-bootstrap'
import { useFormContext, type FieldError } from 'react-hook-form'

type Option = { value: string; label: string }

type Props = {
  name: string
  label: string
  options: Option[]
  placeholder?: string
  disabled?: boolean
  helpText?: string
}

export function SelectField({ name, label, options, placeholder, disabled, helpText }: Props) {
  const {
    register,
    formState: { errors },
  } = useFormContext()
  const fieldError = errors[name] as FieldError | undefined

  return (
    <Form.Group className="mb-3" controlId={name}>
      <Form.Label>{label}</Form.Label>
      <Form.Select
        disabled={disabled}
        isInvalid={Boolean(fieldError)}
        {...register(name)}
      >
        {placeholder && (
          <option value="" disabled>
            {placeholder}
          </option>
        )}
        {options.map((opt) => (
          <option key={opt.value} value={opt.value}>
            {opt.label}
          </option>
        ))}
      </Form.Select>
      {fieldError ? (
        <Form.Control.Feedback type="invalid">
          {String(fieldError.message ?? 'Please select a value')}
        </Form.Control.Feedback>
      ) : helpText ? (
        <Form.Text className="text-muted">{helpText}</Form.Text>
      ) : null}
    </Form.Group>
  )
}
