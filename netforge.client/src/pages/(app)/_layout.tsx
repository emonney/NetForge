import { type ReactNode } from 'react';
import { Navigate, Outlet, useLocation } from 'react-router';
import { useTranslation } from 'react-i18next';

import { useAuth } from '@/hooks/use-auth';
import { FullScreenLoader } from '@/components/full-screen-loader';
import { useDelayedFlag } from '@/hooks/use-delayed-flag';
import { AppSidebar } from '@/components/app/app-sidebar';
import { AppTopbar } from '@/components/app/app-topbar';
import { SidebarInset, SidebarProvider } from '@/components/ui/sidebar';


// Authenticated app shell: a navigation rail (drawer on mobile) + top bar + scrolling content.
// Unauthenticated visitors are sent to /login with a returnUrl so they land back where they headed.
export default function AppLayout() {
  const { isAuthenticated, isLoading } = useAuth();
  const { t } = useTranslation();
  const location = useLocation();
  const showSessionLoader = useDelayedFlag(isLoading);


  // Delayed: a session check that answers quickly should show nothing at all. Without the gate this
  // spinner is the first of three loading states stacked back to back on a cold dashboard load, and
  // it is the one carrying the least information — the shell isn't even up yet to give it a shape.
  if (isLoading) return showSessionLoader ? <FullScreenLoader /> : null;
  if (!isAuthenticated) {
    return <Navigate to={`/login?returnUrl=${encodeURIComponent(location.pathname)}`} replace />;
  }

  // Optional first-run tour — built in TS so the JSX below needs no build-time conditional.
  let onboardingTour: ReactNode = null;

  let realtime: ReactNode = null;

  // Re-tints --primary from the active tenant's brand colour (multi-tenant editions only).
  let tenantBranding: ReactNode = null;

  // Optional full-width banner above the shell (the public demo puts its promo bar here). A banner pins
  // itself to the top of the viewport and publishes its measured height as --app-banner-h; the padding
  // below and the viewport-fixed rail consume that with a 0px fallback, so the shell lays out identically
  // when there is none. Slot your own app-wide announcement in the same way.
  let promoRibbon: ReactNode = null;

  return (
    // Padding, not a wrapper row: `min-h-svh` is border-box, so reserving the banner's height here keeps
    // the shell exactly one viewport tall instead of pushing a scrollbar's worth of overflow past it.
    <SidebarProvider className="pt-[var(--app-banner-h,0px)]">
      {promoRibbon}
      {/* Keyboard users can jump past the nav straight to the page content. */}
      <a
        href="#main-content"
        className="bg-background focus:ring-ring sr-only focus:fixed focus:start-4 focus:top-4 focus:z-50 focus:not-sr-only focus:rounded-md focus:border focus:px-3 focus:py-2 focus:shadow-lg focus:ring-[3px]"
      >
        {t('common.skipToContent')}
      </a>
      {realtime}
      {tenantBranding}
      {onboardingTour}
      <AppSidebar />
      {/* min-w-0: without it the inset keeps its content's min-content width and the shell overflows
          horizontally between md and lg, where the rail is present but the topbar is still crowded. */}
      <SidebarInset className="min-w-0">
        <AppTopbar />
        <div id="main-content" className="mx-auto w-full max-w-6xl flex-1 px-4 py-8 sm:px-6 lg:px-8">
          <Outlet />
        </div>
      </SidebarInset>
    </SidebarProvider>
  );
}
