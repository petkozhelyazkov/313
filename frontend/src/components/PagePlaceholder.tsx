import { Container, Card } from 'react-bootstrap'
import type { ReactNode } from 'react'

type Props = {
  title: string
  epic: string
  children?: ReactNode
}

export function PagePlaceholder({ title, epic, children }: Props) {
  return (
    <Container className="py-4">
      <h1 className="h3">{title}</h1>
      <Card body className="bg-light border-0">
        <div className="text-muted small text-uppercase mb-1">{epic}</div>
        {children ?? (
          <p className="mb-0">
            This screen is a placeholder. Real implementation lands in the epic noted above.
          </p>
        )}
      </Card>
    </Container>
  )
}
