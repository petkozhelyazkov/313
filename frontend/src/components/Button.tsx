import { forwardRef, type ButtonHTMLAttributes, type ReactNode } from 'react'

export type ButtonVariant =
  | 'primary'
  | 'secondary'
  | 'success'
  | 'danger'
  | 'warning'
  | 'info'
  | 'ghost'

export type ButtonSize = 'sm' | 'md' | 'lg'

type ButtonOwnProps = {
  variant?: ButtonVariant
  /** Renders an outlined (transparent) variant that fills on hover. */
  outline?: boolean
  size?: ButtonSize
  /** Stretches the button to the full width of its container. */
  fullWidth?: boolean
  /** Shows a spinner and disables interaction. */
  loading?: boolean
  /** Icon node rendered before children. */
  iconLeft?: ReactNode
  /** Icon node rendered after children. */
  iconRight?: ReactNode
  children?: ReactNode
}

export type ButtonProps = ButtonOwnProps &
  Omit<ButtonHTMLAttributes<HTMLButtonElement>, keyof ButtonOwnProps>

export const Button = forwardRef<HTMLButtonElement, ButtonProps>(function Button(
  {
    variant = 'primary',
    outline = false,
    size = 'md',
    fullWidth,
    loading,
    iconLeft,
    iconRight,
    className,
    disabled,
    type = 'button',
    children,
    ...rest
  },
  ref,
) {
  const classes = [
    'btn-x',
    `btn-x-${variant}`,
    `btn-x-${size}`,
    outline ? 'btn-x-outline' : '',
    fullWidth ? 'btn-x-fullwidth' : '',
    loading ? 'btn-x-loading' : '',
    className,
  ]
    .filter(Boolean)
    .join(' ')

  return (
    <button ref={ref} type={type} className={classes} disabled={disabled || loading} {...rest}>
      {loading && <span className="btn-x-spinner" aria-hidden="true" />}
      {!loading && iconLeft && <span className="btn-x-icon">{iconLeft}</span>}
      {children !== undefined && <span className="btn-x-label">{children}</span>}
      {!loading && iconRight && <span className="btn-x-icon">{iconRight}</span>}
    </button>
  )
})

export type ButtonGroupProps = {
  size?: ButtonSize
  vertical?: boolean
  fullWidth?: boolean
  className?: string
  children?: ReactNode
}

export function ButtonGroup({ size, vertical, fullWidth, className, children }: ButtonGroupProps) {
  const classes = [
    'btn-x-group',
    vertical ? 'btn-x-group-vertical' : '',
    fullWidth ? 'btn-x-group-fullwidth' : '',
    size ? `btn-x-group-${size}` : '',
    className,
  ]
    .filter(Boolean)
    .join(' ')
  return (
    <div className={classes} role="group">
      {children}
    </div>
  )
}
