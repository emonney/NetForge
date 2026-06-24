/**
 * Curated full themes. Each defines the *complete* token set (neutrals + accent) for light AND dark, so
 * switching one re-skins the whole app — backgrounds, surfaces, borders, text — not just the accent. The
 * sidebar tokens are derived from these in the applier. Values are cohesive, contrast-checked HSL.
 *
 * `default` carries no overrides — it falls back to the stylesheet (the built-in slate theme).
 */
export interface ThemeVars {
  background: string;
  foreground: string;
  card: string;
  cardForeground: string;
  popover: string;
  popoverForeground: string;
  primary: string;
  primaryForeground: string;
  secondary: string;
  secondaryForeground: string;
  muted: string;
  mutedForeground: string;
  accent: string;
  accentForeground: string;
  border: string;
  input: string;
  ring: string;
}

export interface Theme {
  key: string;
  name: string;
  /** Representative colour for the gallery swatch. */
  swatch: string;
  /** Absent for `default` (use the stylesheet). */
  light?: ThemeVars;
  dark?: ThemeVars;
}

export const THEMES: Theme[] = [
  { key: 'default', name: 'Default', swatch: 'hsl(222.2 47.4% 11.2%)' },
  {
    key: 'zinc',
    name: 'Zinc',
    swatch: 'hsl(240 5.9% 10%)',
    light: {
      background: 'hsl(0 0% 100%)', foreground: 'hsl(240 10% 3.9%)',
      card: 'hsl(0 0% 100%)', cardForeground: 'hsl(240 10% 3.9%)',
      popover: 'hsl(0 0% 100%)', popoverForeground: 'hsl(240 10% 3.9%)',
      primary: 'hsl(240 5.9% 10%)', primaryForeground: 'hsl(0 0% 98%)',
      secondary: 'hsl(240 4.8% 95.9%)', secondaryForeground: 'hsl(240 5.9% 10%)',
      muted: 'hsl(240 4.8% 95.9%)', mutedForeground: 'hsl(240 3.8% 46.1%)',
      accent: 'hsl(240 4.8% 95.9%)', accentForeground: 'hsl(240 5.9% 10%)',
      border: 'hsl(240 5.9% 90%)', input: 'hsl(240 5.9% 90%)', ring: 'hsl(240 5.9% 10%)',
    },
    dark: {
      background: 'hsl(240 10% 3.9%)', foreground: 'hsl(0 0% 98%)',
      card: 'hsl(240 10% 5.9%)', cardForeground: 'hsl(0 0% 98%)',
      popover: 'hsl(240 10% 5.9%)', popoverForeground: 'hsl(0 0% 98%)',
      primary: 'hsl(0 0% 98%)', primaryForeground: 'hsl(240 5.9% 10%)',
      secondary: 'hsl(240 3.7% 15.9%)', secondaryForeground: 'hsl(0 0% 98%)',
      muted: 'hsl(240 3.7% 15.9%)', mutedForeground: 'hsl(240 5% 64.9%)',
      accent: 'hsl(240 3.7% 15.9%)', accentForeground: 'hsl(0 0% 98%)',
      border: 'hsl(240 3.7% 15.9%)', input: 'hsl(240 3.7% 15.9%)', ring: 'hsl(240 4.9% 83.9%)',
    },
  },
  {
    key: 'ocean',
    name: 'Ocean',
    swatch: 'hsl(217 91% 60%)',
    light: {
      background: 'hsl(210 40% 98%)', foreground: 'hsl(222 47% 11%)',
      card: 'hsl(0 0% 100%)', cardForeground: 'hsl(222 47% 11%)',
      popover: 'hsl(0 0% 100%)', popoverForeground: 'hsl(222 47% 11%)',
      primary: 'hsl(217 91% 60%)', primaryForeground: 'hsl(0 0% 100%)',
      secondary: 'hsl(210 40% 94%)', secondaryForeground: 'hsl(222 47% 11%)',
      muted: 'hsl(210 40% 94%)', mutedForeground: 'hsl(215 16% 47%)',
      accent: 'hsl(210 60% 92%)', accentForeground: 'hsl(222 47% 11%)',
      border: 'hsl(214 32% 88%)', input: 'hsl(214 32% 88%)', ring: 'hsl(217 91% 60%)',
    },
    dark: {
      background: 'hsl(222 47% 8%)', foreground: 'hsl(210 40% 96%)',
      card: 'hsl(222 44% 11%)', cardForeground: 'hsl(210 40% 96%)',
      popover: 'hsl(222 44% 11%)', popoverForeground: 'hsl(210 40% 96%)',
      primary: 'hsl(217 91% 60%)', primaryForeground: 'hsl(222 47% 8%)',
      secondary: 'hsl(217 33% 18%)', secondaryForeground: 'hsl(210 40% 96%)',
      muted: 'hsl(217 33% 18%)', mutedForeground: 'hsl(215 20% 65%)',
      accent: 'hsl(217 40% 22%)', accentForeground: 'hsl(210 40% 96%)',
      border: 'hsl(217 33% 20%)', input: 'hsl(217 33% 20%)', ring: 'hsl(217 91% 60%)',
    },
  },
  {
    key: 'forest',
    name: 'Forest',
    swatch: 'hsl(142 71% 45%)',
    light: {
      background: 'hsl(140 30% 98%)', foreground: 'hsl(150 30% 8%)',
      card: 'hsl(0 0% 100%)', cardForeground: 'hsl(150 30% 8%)',
      popover: 'hsl(0 0% 100%)', popoverForeground: 'hsl(150 30% 8%)',
      primary: 'hsl(142 71% 40%)', primaryForeground: 'hsl(0 0% 100%)',
      secondary: 'hsl(140 25% 94%)', secondaryForeground: 'hsl(150 30% 8%)',
      muted: 'hsl(140 25% 94%)', mutedForeground: 'hsl(150 10% 40%)',
      accent: 'hsl(140 40% 90%)', accentForeground: 'hsl(150 30% 8%)',
      border: 'hsl(140 20% 87%)', input: 'hsl(140 20% 87%)', ring: 'hsl(142 71% 40%)',
    },
    dark: {
      background: 'hsl(150 30% 6%)', foreground: 'hsl(140 30% 96%)',
      card: 'hsl(150 28% 9%)', cardForeground: 'hsl(140 30% 96%)',
      popover: 'hsl(150 28% 9%)', popoverForeground: 'hsl(140 30% 96%)',
      primary: 'hsl(142 71% 45%)', primaryForeground: 'hsl(150 30% 6%)',
      secondary: 'hsl(150 20% 16%)', secondaryForeground: 'hsl(140 30% 96%)',
      muted: 'hsl(150 20% 16%)', mutedForeground: 'hsl(140 15% 65%)',
      accent: 'hsl(150 25% 20%)', accentForeground: 'hsl(140 30% 96%)',
      border: 'hsl(150 20% 18%)', input: 'hsl(150 20% 18%)', ring: 'hsl(142 71% 45%)',
    },
  },
  {
    key: 'sunset',
    name: 'Sunset',
    swatch: 'hsl(20 90% 55%)',
    light: {
      background: 'hsl(30 50% 98%)', foreground: 'hsl(20 30% 10%)',
      card: 'hsl(0 0% 100%)', cardForeground: 'hsl(20 30% 10%)',
      popover: 'hsl(0 0% 100%)', popoverForeground: 'hsl(20 30% 10%)',
      primary: 'hsl(20 90% 52%)', primaryForeground: 'hsl(0 0% 100%)',
      secondary: 'hsl(30 40% 94%)', secondaryForeground: 'hsl(20 30% 10%)',
      muted: 'hsl(30 40% 94%)', mutedForeground: 'hsl(25 10% 44%)',
      accent: 'hsl(30 60% 91%)', accentForeground: 'hsl(20 30% 10%)',
      border: 'hsl(30 30% 88%)', input: 'hsl(30 30% 88%)', ring: 'hsl(20 90% 52%)',
    },
    dark: {
      background: 'hsl(20 30% 7%)', foreground: 'hsl(30 40% 96%)',
      card: 'hsl(20 28% 10%)', cardForeground: 'hsl(30 40% 96%)',
      popover: 'hsl(20 28% 10%)', popoverForeground: 'hsl(30 40% 96%)',
      primary: 'hsl(20 90% 58%)', primaryForeground: 'hsl(20 30% 7%)',
      secondary: 'hsl(20 20% 17%)', secondaryForeground: 'hsl(30 40% 96%)',
      muted: 'hsl(20 20% 17%)', mutedForeground: 'hsl(30 15% 65%)',
      accent: 'hsl(20 25% 21%)', accentForeground: 'hsl(30 40% 96%)',
      border: 'hsl(20 20% 19%)', input: 'hsl(20 20% 19%)', ring: 'hsl(20 90% 58%)',
    },
  },
];

export const THEME_BY_KEY = new Map(THEMES.map((t) => [t.key, t]));
