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

/**
 * The direction for a language tag, regional variants included. Browsers report `ar-EG`, never a bare `ar`,
 * and `nonExplicitSupportedLngs` below resolves that tag's *resources* to the base language while leaving
 * `i18n.language` regional — so matching the tag whole hands real Arabic users Arabic text in an LTR page.
 */
export function directionOf(code: string): 'ltr' | 'rtl' {
  const base = code.toLowerCase().split('-')[0];
  return LANGUAGES.find((l) => l.code === base)?.dir ?? 'ltr';
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
