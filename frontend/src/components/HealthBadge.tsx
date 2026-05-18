import { useEffect, useState } from 'react'
import { Badge } from 'react-bootstrap'
import { apiClient } from '../api/client'

type Status = 'loading' | 'ok' | 'down'

type HealthResponse = {
  status: string
  timestamp: string
  service: string
}

export function HealthBadge() {
  const [status, setStatus] = useState<Status>('loading')
  const [detail, setDetail] = useState<string>('')

  useEffect(() => {
    let cancelled = false
    apiClient
      .get<HealthResponse>('/health')
      .then((res) => {
        if (cancelled) return
        if (res.data?.status === 'ok') {
          setStatus('ok')
          setDetail(`${res.data.service} · ${new Date(res.data.timestamp).toLocaleTimeString()}`)
        } else {
          setStatus('down')
          setDetail('unexpected payload')
        }
      })
      .catch((err: unknown) => {
        if (cancelled) return
        setStatus('down')
        setDetail(err instanceof Error ? err.message : 'unknown error')
      })
    return () => {
      cancelled = true
    }
  }, [])

  const variant = status === 'ok' ? 'success' : status === 'down' ? 'danger' : 'secondary'
  const label = status === 'loading' ? 'API: …' : status === 'ok' ? 'API: ok' : 'API: down'
  const title = detail || undefined

  return (
    <Badge bg={variant} title={title} pill>
      {label}
    </Badge>
  )
}
