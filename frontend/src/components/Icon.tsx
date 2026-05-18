import type { CSSProperties } from 'react'

type IconProps = {
  /** Flaticon UICONS name without the `fi-` prefix. Example: 'rr-bell', 'sr-star'. */
  name: string
  size?: number | string
  className?: string
  style?: CSSProperties
  title?: string
  'aria-hidden'?: boolean
}

/**
 * Thin wrapper around a Flaticon UICONS class. Pass `name="rr-bell"` for the
 * regular-rounded bell, or `name="sr-star"` for the solid-rounded star.
 *
 * Rendered as an inline-flex `<i>` so it can sit naturally inline with text
 * and inherit `color` from its parent.
 */
export function Icon({ name, size = '1em', className, style, title, ...rest }: IconProps) {
  const mergedStyle: CSSProperties = {
    fontSize: typeof size === 'number' ? `${size}px` : size,
    lineHeight: 1,
    display: 'inline-flex',
    alignItems: 'center',
    verticalAlign: '-0.125em',
    ...style,
  }
  return (
    <i
      className={`fi fi-${name}${className ? ` ${className}` : ''}`}
      style={mergedStyle}
      aria-hidden={rest['aria-hidden'] ?? title === undefined}
      title={title}
    />
  )
}
