import { createContext, useCallback, useContext, useRef, useState, type ReactNode } from 'react'
import { Modal } from 'react-bootstrap'
import { Button, type ButtonVariant } from './Button'

export type ConfirmOptions = {
  title?: ReactNode
  body: ReactNode
  confirmLabel?: string
  cancelLabel?: string
  variant?: ButtonVariant
}

type Resolver = (value: boolean) => void

type ConfirmContextValue = (options: ConfirmOptions) => Promise<boolean>

const ConfirmContext = createContext<ConfirmContextValue | null>(null)

export function ConfirmProvider({ children }: { children: ReactNode }) {
  const [options, setOptions] = useState<ConfirmOptions | null>(null)
  const [show, setShow] = useState(false)
  const resolverRef = useRef<Resolver | null>(null)

  const confirm = useCallback<ConfirmContextValue>((opts) => {
    setOptions(opts)
    setShow(true)
    return new Promise<boolean>((resolve) => {
      resolverRef.current = resolve
    })
  }, [])

  const finish = useCallback((value: boolean) => {
    setShow(false)
    if (resolverRef.current) {
      resolverRef.current(value)
      resolverRef.current = null
    }
  }, [])

  const variant = options?.variant ?? 'primary'
  const confirmLabel = options?.confirmLabel ?? 'Confirm'
  const cancelLabel = options?.cancelLabel ?? 'Cancel'

  return (
    <ConfirmContext.Provider value={confirm}>
      {children}
      <Modal
        show={show}
        onHide={() => finish(false)}
        centered
        backdrop="static"
        keyboard
      >
        {options?.title && (
          <Modal.Header closeButton>
            <Modal.Title>{options.title}</Modal.Title>
          </Modal.Header>
        )}
        <Modal.Body>{options?.body}</Modal.Body>
        <Modal.Footer>
          <Button variant="secondary" outline onClick={() => finish(false)}>
            {cancelLabel}
          </Button>
          <Button variant={variant} onClick={() => finish(true)} autoFocus>
            {confirmLabel}
          </Button>
        </Modal.Footer>
      </Modal>
    </ConfirmContext.Provider>
  )
}

/**
 * Returns an `async confirm(options)` function. Resolves true if the user
 * confirms, false if they cancel/dismiss. Replaces native `window.confirm`.
 */
export function useConfirm(): ConfirmContextValue {
  const ctx = useContext(ConfirmContext)
  if (!ctx) throw new Error('useConfirm must be used within ConfirmProvider')
  return ctx
}
