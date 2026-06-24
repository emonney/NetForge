import { type ReactNode } from 'react';
import { Navigate, Outlet, useLocation } from 'react-router';
import { useTranslation } from 'react-i18next';

import { useAuth } from '@/hooks/use-auth';
import { FullScreenLoader } from '@/components/full-screen-loader';
import { AppSidebar } from '@/components/app/app-sidebar';
import { AppTopbar } from '@/components/app/app-topbar';
import { SidebarInset, SidebarProvider } from '@/components/ui/sidebar';


// Authenticated app shell: a navigation rail (drawer on mobile) + top bar + scrolling content.
// Unauthenticated visitors are sent to /login with a returnUrl so they land back where they headed.
export default function AppLayout() {
  const { isAuthenticated, isLoading } = useAuth();
  const { t } = useTranslation();
  const location = useLocation();

  if (isLoading) return <FullScreenLoader />;
  if (!isAuthenticated) {
    return <Navigate to={`/login?returnUrl=${encodeURIComponent(location.pathname)}`} replace />;
  }

  // Optional first-run tour — built in TS so the JSX below needs no build-time conditional.
  let onboardingTour: ReactNode = null;

  let realtime: ReactNode = null;

  // Re-tints --primary from the active tenant's brand colour (multi-tenant editions only).
  let tenantBranding: ReactNode = null;

  return (
    <SidebarProvider>
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
      <SidebarInset>
        <AppTopbar />
        <div id="main-content" className="mx-auto w-full max-w-6xl flex-1 px-4 py-8 sm:px-6 lg:px-8">
          <Outlet />
        </div>
      </SidebarInset>
    </SidebarProvider>
  );
}
