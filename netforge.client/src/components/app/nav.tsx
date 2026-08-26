import { useState, type CSSProperties } from 'react';
import { NavLink, useLocation } from 'react-router';
import { useTranslation } from 'react-i18next';
import { INTRO, introDelay, useIntroMotion } from '@/hooks/use-intro-motion';
import { ChevronRight } from 'lucide-react';

import { useAuth } from '@/hooks/use-auth';
import { hasPermission } from '@/lib/permissions';
import { NAV, type NavItem, type NavSection } from '@/components/app/nav-config';
import { Collapsible, CollapsibleContent, CollapsibleTrigger } from '@/components/ui/collapsible';
import {
  SidebarGroup,
  SidebarGroupContent,
  SidebarGroupLabel,
  SidebarMenu,
  SidebarMenuButton,
  SidebarMenuItem,
  useSidebar,
} from '@/components/ui/sidebar';

/**
 * Primary navigation rendered inside the shadcn <Sidebar>. Each NAV section becomes a SidebarGroup;
 * a *labelled* section is collapsible (its open state persists per user in localStorage), while the
 * unlabelled top section (Home) is always shown. Icons double as tooltips when the rail is collapsed
 * to icons. Selecting an item closes the mobile drawer.
 */
export function SidebarNav() {
  const { user } = useAuth();
  const granted = user?.permissions ?? [];
  // Single-tenant editions hide nothing on this axis; multi-tenant editions hide tenant-only items
  // until the user actually belongs to a switchable tenant.
  let multiTenant = false;

  const sections = NAV.map((section) => ({
    ...section,
    items: section.items.filter(
      (item) =>
        (!item.permission || hasPermission(granted, item.permission)) &&
        (!item.requiresMultiTenant || multiTenant),
    ),
  })).filter((section) => section.items.length > 0);

  // The nav builds in on the first shell paint of a session — once, after signing in, not on every
  // navigation. See useIntroMotion for why that distinction matters.
  const playIntro = useIntroMotion('shell-nav');

  // Where each section starts in one flat running count, so the stagger reads as a single continuous
  // sweep down the rail rather than restarting at every group.
  const startIndexes = sections.reduce<number[]>(
    (acc, _section, i) => [...acc, (acc[i - 1] ?? 0) + (sections[i - 1]?.items.length ?? 0)],
    [],
  );

  return (
    <>
      {sections.map((section, i) => (
        <NavGroup
          key={section.labelKey ?? `s${i}`}
          section={section}
          playIntro={playIntro}
          startIndex={startIndexes[i]}
        />
      ))}
    </>
  );
}

function NavGroup({
  section,
  playIntro,
  startIndex,
}: {
  section: NavSection;
  playIntro: boolean;
  startIndex: number;
}) {
  const menu = (
    <SidebarMenu>
      {section.items.map((item, i) => (
        <NavMenuItem key={item.to} item={item} playIntro={playIntro} introIndex={startIndex + i} />
      ))}
    </SidebarMenu>
  );

  // Unlabelled section (Home): plain group, nothing to collapse.
  if (!section.labelKey) {
    return (
      <SidebarGroup>
        <SidebarGroupContent>{menu}</SidebarGroupContent>
      </SidebarGroup>
    );
  }

  return <CollapsibleNavGroup labelKey={section.labelKey}>{menu}</CollapsibleNavGroup>;
}

function CollapsibleNavGroup({ labelKey, children }: { labelKey: string; children: React.ReactNode }) {
  const { t } = useTranslation();
  const [open, setOpen] = usePersistentOpen(`netforge:nav-group:${labelKey}`, true);

  return (
    <Collapsible open={open} onOpenChange={setOpen} className="group/collapsible">
      <SidebarGroup>
        <SidebarGroupLabel
          asChild
          className="hover:bg-sidebar-accent hover:text-sidebar-accent-foreground cursor-pointer"
        >
          <CollapsibleTrigger>
            {t(labelKey)}
            <ChevronRight className="ms-auto size-3.5 transition-transform group-data-[state=open]/collapsible:rotate-90" />
          </CollapsibleTrigger>
        </SidebarGroupLabel>
        <CollapsibleContent>
          <SidebarGroupContent>{children}</SidebarGroupContent>
        </CollapsibleContent>
      </SidebarGroup>
    </Collapsible>
  );
}

function NavMenuItem({
  item,
  playIntro,
  introIndex,
}: {
  item: NavItem;
  playIntro: boolean;
  introIndex: number;
}) {
  const { t } = useTranslation();
  const { pathname } = useLocation();
  const { isMobile, setOpenMobile } = useSidebar();

  // Fixed on mount. introDelay() is relative to *now*, so recomputing it on a later re-render would
  // hand CSS a shorter delay mid-animation and restart the item from the top.
  const [delay] = useState(() => introDelay(INTRO.nav, introIndex));
  const introProps = playIntro
    ? {
        className: 'nav-intro-item',
        style: {
          '--intro-delay': `${delay}ms`,
          '--intro-duration': `${INTRO.nav.duration}ms`,
        } as CSSProperties,
      }
    : {};

  // External items (e.g. the Hangfire dashboard) are server-rendered pages, not SPA routes — open them
  // in a new tab so the app stays put; same origin, so the auth cookie rides along.
  if (item.external) {
    return (
      <SidebarMenuItem {...introProps}>
        <SidebarMenuButton asChild tooltip={t(item.titleKey)}>
          <a
            href={item.to}
            target="_blank"
            rel="noopener noreferrer"
            onClick={() => isMobile && setOpenMobile(false)}
          >
            <item.icon />
            <span>{t(item.titleKey)}</span>
          </a>
        </SidebarMenuButton>
      </SidebarMenuItem>
    );
  }

  const active = item.end
    ? pathname === item.to
    : pathname === item.to || pathname.startsWith(`${item.to}/`);

  return (
    <SidebarMenuItem {...introProps}>
      <SidebarMenuButton asChild isActive={active} tooltip={t(item.titleKey)}>
        <NavLink to={item.to} end={item.end} viewTransition onClick={() => isMobile && setOpenMobile(false)}>
          <item.icon />
          <span>{t(item.titleKey)}</span>
        </NavLink>
      </SidebarMenuButton>
    </SidebarMenuItem>
  );
}

/** Open/closed state backed by localStorage, so a collapsed group stays collapsed across reloads. */
function usePersistentOpen(key: string, fallback: boolean): [boolean, (next: boolean) => void] {
  const [open, setOpen] = useState(() => {
    const stored = localStorage.getItem(key);
    return stored === null ? fallback : stored === 'true';
  });
  const set = (next: boolean) => {
    setOpen(next);
    localStorage.setItem(key, String(next));
  };
  return [open, set];
}
