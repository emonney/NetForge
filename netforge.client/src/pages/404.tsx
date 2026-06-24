import { Link, useLocation, useNavigate } from 'react-router';
import { useTranslation } from 'react-i18next';
import { ArrowLeft, Home } from 'lucide-react';

import { useDocumentTitle } from '@/hooks/use-document-title';
import { Button } from '@/components/ui/button';
import { Brand } from '@/components/brand';
import { ModeToggle } from '@/components/mode-toggle';

// Custom not-found page (generouted maps 404.tsx to the catch-all route). It renders standalone —
// outside the app shell — so it carries its own brand mark + theme toggle and centers a designed state
// (§7.0), rather than the bare unstyled fallback it replaced.
export default function NotFound() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const { pathname } = useLocation();
  useDocumentTitle(t('notFound.title'));

  return (
    <div className="bg-background text-foreground relative grid min-h-svh place-items-center px-4">
      <div className="absolute start-4 top-4">
        <Brand />
      </div>
      <div className="absolute end-4 top-4">
        <ModeToggle />
      </div>

      <main className="flex w-full max-w-md flex-col items-center text-center">
        <p
          aria-hidden
          className="from-foreground to-muted-foreground bg-gradient-to-b bg-clip-text text-[5.5rem] leading-none font-extrabold tracking-tight text-transparent select-none sm:text-[7rem]"
        >
          404
        </p>
        <h1 className="mt-2 text-2xl font-semibold tracking-tight">{t('notFound.title')}</h1>
        <p className="text-muted-foreground mt-2 text-balance">{t('notFound.description')}</p>
        {pathname && pathname !== '/' && (
          <code className="bg-muted text-muted-foreground mt-4 max-w-full truncate rounded-md px-2 py-1 font-mono text-xs">
            {pathname}
          </code>
        )}
        <div className="mt-6 flex flex-wrap items-center justify-center gap-3">
          <Button variant="outline" onClick={() => navigate(-1)}>
            <ArrowLeft />
            {t('notFound.goBack')}
          </Button>
          <Button asChild>
            <Link to="/">
              <Home />
              {t('notFound.goHome')}
            </Link>
          </Button>
        </div>
      </main>
    </div>
  );
}
