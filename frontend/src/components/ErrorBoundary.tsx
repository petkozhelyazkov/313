import { Component, type ErrorInfo, type ReactNode } from 'react'
import { Container } from 'react-bootstrap'
import { withTranslation, type WithTranslation } from 'react-i18next'
import { Button } from './Button'

type Props = WithTranslation & { children: ReactNode }
type State = { error: Error | null }

class ErrorBoundaryInner extends Component<Props, State> {
  state: State = { error: null }

  static getDerivedStateFromError(error: Error): State {
    return { error }
  }

  componentDidCatch(error: Error, info: ErrorInfo): void {
    console.error('Unhandled error:', error, info)
  }

  handleReload = () => {
    window.location.reload()
  }

  render() {
    if (this.state.error) {
      const { t } = this.props
      return (
        <Container className="py-5 text-center">
          <h1 className="h3">{t('error.generic')}</h1>
          <p className="text-muted">{t('error.render')}</p>
          <pre className="text-start small bg-light p-3 rounded border d-inline-block mt-3 mb-4">
            {this.state.error.message}
          </pre>
          <div>
            <Button variant="primary" onClick={this.handleReload}>
              {t('error.reload')}
            </Button>
          </div>
        </Container>
      )
    }
    return this.props.children
  }
}

export const ErrorBoundary = withTranslation()(ErrorBoundaryInner)
