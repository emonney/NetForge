import i18n from 'i18next';
import { initReactI18next } from 'react-i18next';
import LanguageDetector from 'i18next-browser-languagedetector';

import en from './locales/en.json';
import es from './locales/es.json';
import fr from './locales/fr.json';
import de from './locales/de.json';
import ar from './locales/ar.json';
import zh from './locales/zh.json';

export type LanguageMeta = { code: string; name: string; dir: 'ltr' | 'rtl' };

// Single source of truth for supported languages. To add one: drop `src/locales/<code>.json`,
// import it into `resources` below, and add an entry here (set `dir: 'rtl'` for RTL scripts). The
// `name` is the language's own autonym so the switcher reads natively regardless of current locale.
export const LANGUAGES: LanguageMeta[] = [
  { code: 'en', name: 'English', dir: 'ltr' },
  { code: 'es', name: 'Español', dir: 'ltr' },
  { code: 'fr', name: 'Français', dir: 'ltr' },
  { code: 'de', name: 'Deutsch', dir: 'ltr' },
  { code: 'ar', name: 'العربية', dir: 'rtl' },
  { code: 'zh', name: '中文', dir: 'ltr' },
];

export const supportedLngs = LANGUAGES.map((l) => l.code);

export function directionOf(code: string): 'ltr' | 'rtl' {
  return LANGUAGES.find((l) => l.code === code)?.dir ?? 'ltr';
}

i18n
  .use(LanguageDetector)
  .use(initReactI18next)
  .init({
    resources: {
      en: { translation: en },
      es: { translation: es },
      fr: { translation: fr },
      de: { translation: de },
      ar: { translation: ar },
      zh: { translation: zh },
    },
    fallbackLng: 'en',
    supportedLngs,
    nonExplicitSupportedLngs: true, // map regional tags (en-US → en) to a base language
    interpolation: { escapeValue: false }, // React already escapes
    detection: {
      order: ['localStorage', 'navigator'],
      caches: ['localStorage'],
      lookupLocalStorage: 'netforge-lang',
    },
  });

export default i18n;
