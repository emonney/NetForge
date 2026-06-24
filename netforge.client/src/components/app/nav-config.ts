import {
  Activity,
  Home,
  KeyRound,
  Settings,
  Shield,
  Users,
  type LucideIcon,
} from 'lucide-react';

import { PERM } from '@/lib/api/admin';
import { HEALTH_PERM } from '@/lib/api/health';

export type NavItem = {
  titleKey: string;
  to: string;
  icon: LucideIcon;
  permission?: string;
  end?: boolean;
  /** Hidden unless multi-tenant mode is active — single-tenant devs never see tenant UI. */
  requiresMultiTenant?: boolean;
  /** Opens a server-rendered page (e.g. the Hangfire dashboard) in a new tab — not a SPA route. */
  external?: boolean;
};
export type NavSection = { labelKey?: string; items: NavItem[] };

// The app's primary navigation. New features add entries here; `permission` gates an item to users
// who hold it (the route + API enforce it for real). Titles are i18n keys resolved at render so the
// nav follows the active language. Shared by the sidebar (SidebarNav) and the command palette.
export const NAV: NavSection[] = [
  {
    items: [{ titleKey: 'nav.home', to: '/', icon: Home, end: true }],
  },
  {
    labelKey: 'nav.administration',
    items: [
      { titleKey: 'nav.users', to: '/admin/users', icon: Users, permission: PERM.usersRead },
      { titleKey: 'nav.roles', to: '/admin/roles', icon: Shield, permission: PERM.rolesRead },
      { titleKey: 'nav.permissions', to: '/admin/permissions', icon: KeyRound, permission: PERM.rolesRead },
      { titleKey: 'nav.settings', to: '/admin/settings', icon: Settings, permission: PERM.settingsRead },
      { titleKey: 'nav.health', to: '/admin/health', icon: Activity, permission: HEALTH_PERM.read },
    ],
  },
];
