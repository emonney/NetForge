/* eslint-disable react-refresh/only-export-components -- the theme applier co-exports its helper functions alongside the component by design */
import { useEffect } from 'react';
import { useQuery } from '@tanstack/react-query';

import { appearanceApi } from '@/lib/api/appearance';
import { DEFAULT_BRAND_COLOR, DEFAULT_THEME } from '@/lib/brand';
import { THEME_BY_KEY, type ThemeVars } from '@/lib/themes';

const STYLE_ID = 'nf-appearance';

// The scaffold-time theme + accent, used until an admin sets one at runtime.
const SCAFFOLD_BRAND = isColor(DEFAULT_BRAND_COLOR) ? DEFAULT_BRAND_COLOR : null;
const SCAFFOLD_THEME = THEME_BY_KEY.has(DEFAULT_THEME) ? DEFAULT_THEME : null;

/**
 * Applies the instance appearance app-wide by injecting a `<style>` block that overrides the design tokens —
 * a full curated theme (complete light + dark palettes) and/or a custom accent. Reads the anonymous
 * `/api/appearance`, so it themes the pre-auth screens too. Mounted once at the app root; renders nothing.
 */
export function BrandColor() {
  const { data } = useQuery({ queryKey: ['appearance'], queryFn: appearanceApi.get, staleTime: 5 * 60_000 });
  const themeKey = data?.theme ?? SCAFFOLD_THEME;
  // The scaffold-time accent is only the *initial* default. Once a theme is explicitly chosen it must drop out,
  // otherwise it paints over every theme's own accent forever. An explicit appearance accent — or a tenant
  // brand colour, which arrives as `brandColor` from the API — still wins.
  const accent = data?.brandColor ?? (data?.theme ? null : SCAFFOLD_BRAND);
  const customTheme = data?.customTheme ?? null;

  useEffect(() => {
    const css = buildCss(themeKey, accent, customTheme);
    let el = document.getElementById(STYLE_ID) as HTMLStyleElement | null;
    if (!css) {
      el?.remove();
      return;
    }
    if (!el) {
      el = document.createElement('style');
      el.id = STYLE_ID;
      document.head.appendChild(el);
    }
    el.textContent = css;
  }, [themeKey, accent, customTheme]);

  return null;
}

/** Build the override stylesheet for the chosen theme (or a custom palette) + accent. */
export function buildCss(themeKey: string | null, accent: string | null, customTheme?: string | null): string {
  const accentOk = accent && isColor(accent) ? accent : null;

  let light: ThemeVars | undefined;
  let dark: ThemeVars | undefined;
  if (themeKey === 'custom' && customTheme) {
    const custom = parseCustomTheme(customTheme);
    light = custom?.light;
    dark = custom?.dark;
  } else if (themeKey) {
    const theme = THEME_BY_KEY.get(themeKey);
    light = theme?.light;
    dark = theme?.dark;
  }
  if (!light && !accentOk) return '';

  const lightCss = light ? fullBlock(light, accentOk) : accentOk ? accentBlock(accentOk) : '';
  const darkCss = dark ? fullBlock(dark, accentOk) : accentOk ? accentBlock(accentOk) : '';
  return `:root{${lightCss}}\n.dark{${darkCss}}`;
}

/** Parse a custom palette JSON ({ light, dark }); returns null if malformed. */
export function parseCustomTheme(json: string): { light: ThemeVars; dark: ThemeVars } | null {
  try {
    const p = JSON.parse(json);
    if (p && typeof p === 'object' && p.light && p.dark) return p as { light: ThemeVars; dark: ThemeVars };
  } catch {
    /* malformed — ignore */
  }
  return null;
}

/** A complete token set from a theme palette, with the accent overriding the primary/ring if provided. */
function fullBlock(v: ThemeVars, accent: string | null): string {
  const p = accent ?? v.primary;
  const r = accent ?? v.ring;
  return (
    [
      `--background:${v.background}`,
      `--foreground:${v.foreground}`,
      `--card:${v.card}`,
      `--card-foreground:${v.cardForeground}`,
      `--popover:${v.popover}`,
      `--popover-foreground:${v.popoverForeground}`,
      `--primary:${p}`,
      `--primary-foreground:${v.primaryForeground}`,
      `--secondary:${v.secondary}`,
      `--secondary-foreground:${v.secondaryForeground}`,
      `--muted:${v.muted}`,
      `--muted-foreground:${v.mutedForeground}`,
      `--accent:${v.accent}`,
      `--accent-foreground:${v.accentForeground}`,
      `--border:${v.border}`,
      `--input:${v.input}`,
      `--ring:${r}`,
      // sidebar derived from the themed surface so the rail blends in
      `--sidebar:${v.background}`,
      `--sidebar-foreground:${v.foreground}`,
      `--sidebar-primary:${p}`,
      `--sidebar-primary-foreground:${v.primaryForeground}`,
      `--sidebar-accent:${v.accent}`,
      `--sidebar-accent-foreground:${v.accentForeground}`,
      `--sidebar-border:${v.border}`,
      `--sidebar-ring:${r}`,
    ].join(';') + ';'
  );
}

/** Accent-only override (no base theme): just the primary/ring family. */
function accentBlock(accent: string): string {
  return `--primary:${accent};--ring:${accent};--sidebar-primary:${accent};--sidebar-ring:${accent};`;
}

/** Only apply values the browser recognizes as a colour (the API validates too — defense in depth). */
export function isColor(value: string): boolean {
  if (value.length > 64) return false;
  return typeof CSS === 'undefined' || CSS.supports('color', value);
}
