import type { ComponentType } from 'react';
import { useTranslation } from 'react-i18next';
import { Navigate, Outlet, useSearchParams } from 'react-router';
import { Globe, ShieldCheck, Zap } from 'lucide-react';

import { useAuth } from '@/hooks/use-auth';
import { FullScreenLoader } from '@/components/full-screen-loader';
import { Brand } from '@/components/brand';
import { ModeToggle } from '@/components/mode-toggle';
import { LanguageToggle } from '@/components/language-toggle';

// Shared shell for the unauthenticated screens. Already-signed-in visitors are bounced to their
// destination (returnUrl) or the app root. Split layout: brand showcase left, form right; on
// mobile the showcase drops and the form centers.
export default function AuthLayout() {
  const { isAuthenticated, isLoading } = useAuth();
  const [params] = useSearchParams();

  if (isLoading) return <FullScreenLoader />;
  if (isAuthenticated) {
    const returnUrl = params.get('returnUrl');
    return <Navigate to={returnUrl && returnUrl.startsWith('/') ? returnUrl : '/'} replace />;
  }

  return (
    <div className="grid min-h-svh lg:grid-cols-2">
      <BrandPanel />
      <div className="relative flex flex-col items-center justify-center px-4 py-12 sm:px-8">
        <div className="absolute end-4 top-4 flex items-center gap-1">
          <LanguageToggle />
          <ModeToggle />
        </div>
        <div className="mb-8 lg:hidden">
          <Brand className="text-lg" />
        </div>
        <main className="w-full max-w-sm">
          <Outlet />
        </main>
      </div>
    </div>
  );
}

// Always-dark showcase: a branded surface that stays rich in both themes (it must not follow the
// theme's primary token, which inverts to near-white in dark mode).
function BrandPanel() {
  const { t } = useTranslation();
  return (
    <div className="relative hidden flex-col justify-between overflow-hidden bg-gradient-to-b from-slate-900 to-slate-950 p-12 text-slate-50 lg:flex">
      <div
        aria-hidden
        className="pointer-events-none absolute -top-1/4 -right-1/4 size-[40rem] rounded-full bg-white/5 blur-3xl"
      />
      <Brand tone="onDark" className="relative text-lg" />

      <div className="relative max-w-md space-y-6">
        <h1 className="text-3xl leading-tight font-semibold tracking-tight text-balance">
          {t('auth.brand.tagline')}
        </h1>
        <p className="text-balance text-slate-300">{t('auth.brand.subtitle', { year: new Date().getFullYear() })}</p>
        <ul className="space-y-3 text-sm text-slate-200">
          <Feature icon={ShieldCheck}>{t('auth.brand.feature1')}</Feature>
          <Feature icon={Zap}>{t('auth.brand.feature2')}</Feature>
          <Feature icon={Globe}>{t('auth.brand.feature3')}</Feature>
        </ul>
      </div>

      <p className="relative text-xs text-slate-500">© {new Date().getFullYear()} NetForge</p>
    </div>
  );
}

function Feature({ icon: Icon, children }: { icon: ComponentType<{ className?: string }>; children: React.ReactNode }) {
  return (
    <li className="flex items-center gap-3">
      <span className="grid size-8 place-items-center rounded-lg bg-white/10">
        <Icon className="size-4" />
      </span>
      {children}
    </li>
  );
}
