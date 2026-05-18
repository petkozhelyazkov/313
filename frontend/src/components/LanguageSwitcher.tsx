import { NavDropdown } from 'react-bootstrap'
import { useTranslation } from 'react-i18next'
import { SUPPORTED_LANGUAGES } from '../i18n'
import { Icon } from './Icon'

export function LanguageSwitcher() {
  const { i18n } = useTranslation()
  const current = i18n.resolvedLanguage ?? i18n.language ?? 'en'
  const label = SUPPORTED_LANGUAGES.find((l) => l.code === current)?.label ?? 'English'

  return (
    <NavDropdown
      align="end"
      title={<><Icon name="rr-globe" className="me-1" /> {label}</>}
      id="lang-switcher"
      menuVariant="dark"
    >
      {SUPPORTED_LANGUAGES.map((lang) => (
        <NavDropdown.Item
          key={lang.code}
          active={lang.code === current}
          onClick={() => i18n.changeLanguage(lang.code)}
        >
          {lang.label}
        </NavDropdown.Item>
      ))}
    </NavDropdown>
  )
}
