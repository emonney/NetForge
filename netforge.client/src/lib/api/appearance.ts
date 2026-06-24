import { api } from './client';

/** Permission gating brand-colour changes — matches Features/Appearance/Permissions.cs. */
export const APPEARANCE_PERM = { manage: 'appearance.manage' } as const;

export interface Appearance {
  /** Curated theme key (e.g. "ocean"), "custom" for a user palette, or null for the built-in theme. */
  theme: string | null;
  /** Any CSS colour overriding the theme accent, or null. */
  brandColor: string | null;
  /** A user-defined palette as JSON ({ light, dark }), applied when theme === "custom". */
  customTheme: string | null;
}

export type AppearanceUpdate = { theme: string | null; brandColor: string | null; customTheme: string | null };

export const appearanceApi = {
  get: () => api.get<Appearance>('/appearance/'),
  update: (body: AppearanceUpdate) => api.put<Appearance>('/appearance/', body),
};
