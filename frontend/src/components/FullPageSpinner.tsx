import { Spinner } from 'react-bootstrap'

export function FullPageSpinner() {
  return (
    <div className="d-flex justify-content-center align-items-center" style={{ minHeight: '100vh' }}>
      <Spinner animation="border" role="status" variant="primary">
        <span className="visually-hidden">Loading…</span>
      </Spinner>
    </div>
  )
}
