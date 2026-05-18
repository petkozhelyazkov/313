import { createContext, useCallback, useContext, useEffect, useState, type ReactNode } from 'react'

export type Theme = 'light' | 'dark' | 'system'

type ThemeContextValue = {
  theme: Theme
  resolvedTheme: 'light' | 'dark'
  setTheme: (t: Theme) => void
}

const ThemeContext = createContext<ThemeContextValue | null>(null)
const STORAGE_KEY = 'trading212.theme'

function readStored(): Theme {
  try {
    const v = localStorage.getItem(STORAGE_KEY)
    if (v === 'light' || v === 'dark' || v === 'system') return v
  } catch {
    /* ignore */
  }
  return 'system'
}

function systemPrefersDark(): boolean {
  return typeof window !== 'undefined'
    && window.matchMedia
    && window.matchMedia('(prefers-color-scheme: dark)').matches
}

function applyTheme(resolved: 'light' | 'dark') {
  document.documentElement.setAttribute('data-bs-theme', resolved)
  document.documentElement.classList.toggle('theme-dark', resolved === 'dark')
}

export function ThemeProvider({ children }: { children: ReactNode }) {
  const [theme, setThemeState] = useState<Theme>(() => readStored())
  const [resolvedTheme, setResolvedTheme] = useState<'light' | 'dark'>(() =>
    theme === 'system' ? (systemPrefersDark() ? 'dark' : 'light') : (theme as 'light' | 'dark'),
  )

  useEffect(() => {
    const resolved = theme === 'system' ? (systemPrefersDark() ? 'dark' : 'light') : theme
    setResolvedTheme(resolved)
    applyTheme(resolved)
    try {
      localStorage.setItem(STORAGE_KEY, theme)
    } catch {
      /* ignore */
    }
  }, [theme])

  // React to system theme changes when user has theme=system
  useEffect(() => {
    if (theme !== 'system' || !window.matchMedia) return
    const mq = window.matchMedia('(prefers-color-scheme: dark)')
    const handler = () => {
      const r = mq.matches ? 'dark' : 'light'
      setResolvedTheme(r)
      applyTheme(r)
    }
    mq.addEventListener('change', handler)
    return () => mq.removeEventListener('change', handler)
  }, [theme])

  const setTheme = useCallback((t: Theme) => setThemeState(t), [])

  return (
    <ThemeContext.Provider value={{ theme, resolvedTheme, setTheme }}>
      {children}
    </ThemeContext.Provider>
  )
}

export function useTheme() {
  const ctx = useContext(ThemeContext)
  if (!ctx) throw new Error('useTheme must be used within ThemeProvider')
  return ctx
}
