import { useEffect, useState } from 'react'
import { Card, Form } from 'react-bootstrap'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'
import { toast } from 'react-hot-toast'
import { useAuth } from '../auth/useAuth'
import { useUpdatePreferences } from '../api/digests'
import { getApiErrorMessage } from '../api/client'

export function PreferencesCard() {
  const { t } = useTranslation()
  const { user, refreshUser } = useAuth()
  const update = useUpdatePreferences()
  const [enabled, setEnabled] = useState<boolean>(user?.emailDigestEnabled ?? true)

  useEffect(() => {
    if (user?.emailDigestEnabled !== undefined) {
      setEnabled(user.emailDigestEnabled)
    }
  }, [user?.emailDigestEnabled])

  const toggle = async (value: boolean) => {
    setEnabled(value)
    try {
      await update.mutateAsync({ emailDigestEnabled: value })
      await refreshUser()
      toast.success(t('digests.savedPrefs'))
    } catch (err) {
      setEnabled(!value)
      toast.error(getApiErrorMessage(err))
    }
  }

  return (
    <Card>
      <Card.Header><strong>{t('digests.preferences')}</strong></Card.Header>
      <Card.Body>
        <Form.Check
          type="switch"
          id="email-digest-switch"
          checked={enabled}
          onChange={(e) => toggle(e.target.checked)}
          label={t('digests.optInLabel')}
        />
        <small className="text-muted d-block mt-2">{t('digests.optInHint')}</small>
        <div className="mt-3">
          <Link to="/digests" className="btn btn-link p-0">{t('digests.viewAll')} →</Link>
        </div>
      </Card.Body>
    </Card>
  )
}
