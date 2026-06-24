import { type ReactNode } from 'react';

import { ModeToggle } from '@/components/mode-toggle';
import { LanguageToggle } from '@/components/language-toggle';
import { UserMenu } from '@/components/app/user-menu';
import { Separator } from '@/components/ui/separator';
import { SidebarTrigger } from '@/components/ui/sidebar';

// App top bar (inside <SidebarInset>): the sidebar collapse toggle, an optional tenant switcher, a
// prominent centred command palette (the multi-purpose search), and the theme/notifications/account
// controls. The mobile nav drawer is the sidebar's own off-canvas sheet, opened by the same trigger.
export function AppTopbar() {
  // Optional topbar pieces — built in TS so the JSX needs no build-time conditional.
  let whatsNew: ReactNode = null;

  let commandPalette: ReactNode = null;

  let notificationBell: ReactNode = null;

  let tenantSwitcher: ReactNode = null;

  return (
    <header className="bg-background/80 sticky top-0 z-30 flex h-14 items-center gap-2 border-b px-3 backdrop-blur sm:px-4">
      <SidebarTrigger className="text-muted-foreground" />
      <Separator orientation="vertical" className="me-1 hidden h-5 sm:block" />

      {tenantSwitcher}

      {/* Prominent, centred search — the multi-purpose command palette (VS Code-style). */}
      <div className="flex min-w-0 flex-1 justify-center px-1 sm:px-2">
        <span data-tour="command" className="w-full max-w-md">
          {commandPalette}
        </span>
      </div>

      <div className="flex items-center gap-0.5 sm:gap-1">
        {whatsNew}
        {notificationBell}
        <LanguageToggle />
        <span data-tour="theme" className="flex items-center">
          <ModeToggle />
        </span>
        <span data-tour="account" className="flex items-center">
          <UserMenu />
        </span>
      </div>
    </header>
  );
}
