/** Curated brand-colour presets for the appearance picker. Each is mid-tone so the default light/dark
 * `--primary-foreground` (near-white) stays legible on it. `null` = the stylesheet default. */
export interface ThemePreset {
  name: string;
  /** A CSS colour, or null for the built-in default. */
  color: string | null;
}

export const THEME_PRESETS: ThemePreset[] = [
  { name: 'Default', color: null },
  { name: 'Indigo', color: '#4f46e5' },
  { name: 'Violet', color: '#7c3aed' },
  { name: 'Blue', color: '#2563eb' },
  { name: 'Emerald', color: '#059669' },
  { name: 'Teal', color: '#0d9488' },
  { name: 'Rose', color: '#e11d48' },
  { name: 'Orange', color: '#ea580c' },
  { name: 'Slate', color: '#475569' },
];
