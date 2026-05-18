import { useFormContext } from 'react-hook-form'
import type { ReactNode } from 'react'
import { Button, type ButtonVariant } from '../Button'

type Props = {
  children: ReactNode
  variant?: ButtonVariant
  disabled?: boolean
  className?: string
  fullWidth?: boolean
}

export function FormSubmitButton({
  children,
  variant = 'primary',
  disabled,
  className,
  fullWidth,
}: Props) {
  const {
    formState: { isSubmitting },
  } = useFormContext()

  return (
    <Button
      type="submit"
      variant={variant}
      disabled={disabled}
      loading={isSubmitting}
      fullWidth={fullWidth}
      className={className}
    >
      {children}
    </Button>
  )
}
