import { useEffect, type ReactNode } from 'react';
import { Outlet } from 'react-router';
import { QueryClientProvider } from '@tanstack/react-query';

import { useAuth } from '@/hooks/use-auth';
import { ThemeProvider } from '@/components/theme-provider';
import { BrandColor } from '@/components/app/brand-color';
import { Toaster } from '@/components/ui/sonner';
import { queryClient } from '@/lib/query-client';
import i18n, { directionOf } from '@/i18n.config';

// Keep <html lang> and <html dir> in sync with the active language so the page mirrors correctly for
// RTL scripts (e.g. Arabic). Logical CSS properties throughout do the rest of the flipping.
function useDocumentLanguage() {
  useEffect(() => {
    const apply = (lng: string) => {
      const root = document.documentElement;
      root.lang = lng;
      root.dir = directionOf(lng);
    };
    apply(i18n.language);
    i18n.on('languageChanged', apply);
    return () => i18n.off('languageChanged', apply);
  }, []);
}

// Once signed in, adopt the user's saved language so their preference follows them across devices.
function AuthLocaleSync() {
  const { user } = useAuth();
  useEffect(() => {
    if (user?.locale && i18n.resolvedLanguage !== user.locale) i18n.changeLanguage(user.locale);
  }, [user?.locale]);
  return null;
}

// App-level layout: global providers wrap every route.
export default function App() {
  useDocumentLanguage();

  // Optional shell extras are built here (guardable in plain TS) and rendered as a node below, so the
  // JSX tree never needs a build-time conditional — premium tiers add to it, Basic leaves it null.
  const slots: Record<string, ReactNode> = {};

  return (
    <QueryClientProvider client={queryClient}>
      <ThemeProvider defaultTheme="system" storageKey="netforge-theme">
        <BrandColor />
        <AuthLocaleSync />
        <Outlet />
        <Toaster />
        {slots.pwaPrompts}
      </ThemeProvider>
    </QueryClientProvider>
  );
}
