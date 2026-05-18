import { NavDropdown } from 'react-bootstrap'
import { useTheme, type Theme } from '../theme/ThemeContext'
import { Icon } from './Icon'

const OPTIONS: { value: Theme; label: string; icon: string }[] = [
  { value: 'light', label: 'Light', icon: 'rr-sun' },
  { value: 'dark', label: 'Dark', icon: 'rr-moon' },
  { value: 'system', label: 'System', icon: 'rr-computer' },
]

export function ThemeToggle() {
  const { theme, resolvedTheme, setTheme } = useTheme()
  const current = OPTIONS.find((o) => o.value === theme) ?? OPTIONS[2]
  return (
    <NavDropdown
      align="end"
      menuVariant={resolvedTheme === 'dark' ? 'dark' : undefined}
      title={<Icon name={current.icon} size={18} title="Theme" />}
      id="theme-toggle"
    >
      {OPTIONS.map((opt) => (
        <NavDropdown.Item
          key={opt.value}
          active={theme === opt.value}
          onClick={() => setTheme(opt.value)}
        >
          <Icon name={opt.icon} className="me-2" /> {opt.label}
        </NavDropdown.Item>
      ))}
    </NavDropdown>
  )
}
