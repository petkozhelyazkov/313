import i18n from 'i18next'
import { initReactI18next } from 'react-i18next'
import LanguageDetector from 'i18next-browser-languagedetector'
import enJson from './locales/en.json'
import bgJson from './locales/bg.json'

export const SUPPORTED_LANGUAGES = [
  { code: 'en', label: 'English' },
  { code: 'bg', label: 'Български' },
] as const

void i18n
  .use(LanguageDetector)
  .use(initReactI18next)
  .init({
    resources: {
      en: { translation: enJson },
      bg: { translation: bgJson },
    },
    fallbackLng: 'en',
    supportedLngs: ['en', 'bg'],
    interpolation: { escapeValue: false },
    detection: {
      order: ['localStorage', 'navigator'],
      caches: ['localStorage'],
      lookupLocalStorage: 'trading212.lang',
    },
  })

export default i18n
