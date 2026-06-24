import { useTranslation } from 'react-i18next';
import { Link, useSearchParams } from 'react-router';
import { useQuery } from '@tanstack/react-query';
import { CircleCheck, Loader2, TriangleAlert } from 'lucide-react';

import { authApi } from '@/lib/api/auth';
import { useDocumentTitle } from '@/hooks/use-document-title';
import { Button } from '@/components/ui/button';
import { meta } from './meta';

export default function VerifyEmailPage() {
  const { t } = useTranslation();
  useDocumentTitle(meta.title);
  const [params] = useSearchParams();
  const userId = params.get('userId');
  const token = params.get('token');

  // Confirmation is modelled as a query, not a mutation-fired-from-an-effect. That earlier pattern
  // got permanently stuck on the spinner: under StrictMode's mount→unmount→remount, the run-once ref
  // guard suppressed the re-fire while the mutation observer reset to idle, so a *successful* (200)
  // confirmation never surfaced as success. A query keeps its outcome in the query cache keyed by the
  // link, so it survives the remount and any re-render — it resolves once and the result sticks. The
  // confirm endpoint is idempotent server-side, so even a retry is harmless.
  const confirm = useQuery({
    queryKey: ['confirm-email', userId, token],
    queryFn: () => authApi.confirmEmail({ userId: userId!, token: token! }),
    enabled: !!userId && !!token,
    retry: false,
    staleTime: Infinity,
    gcTime: Infinity,
  });

  if (!userId || !token) {
    return (
      <State
        tone="error"
        icon={<TriangleAlert className="size-6" />}
        title={t('auth.verify.invalidTitle')}
        body={t('auth.verify.invalidDesc')}
        action={<Link to="/register">{t('auth.verify.backToSignUp')}</Link>}
      />
    );
  }

  if (confirm.isSuccess) {
    return (
      <State
        tone="success"
        icon={<CircleCheck className="size-6" />}
        title={t('auth.verify.confirmedTitle')}
        body={t('auth.verify.confirmedDesc')}
        action={<Link to="/login">{t('auth.verify.goSignIn')}</Link>}
        primary
      />
    );
  }

  if (confirm.isError) {
    return (
      <State
        tone="error"
        icon={<TriangleAlert className="size-6" />}
        title={t('auth.verify.failedTitle')}
        body={t('auth.verify.failedDesc')}
        action={<Link to="/login">{t('auth.backToSignIn')}</Link>}
      />
    );
  }

  return (
    <div className="grid gap-4 text-center" role="status" aria-live="polite">
      <Loader2 className="text-muted-foreground mx-auto size-8 animate-spin" />
      <h1 className="text-xl font-semibold tracking-tight">{t('auth.verify.confirmingTitle')}</h1>
      <p className="text-muted-foreground text-sm">{t('auth.verify.confirmingDesc')}</p>
    </div>
  );
}

function State({
  tone,
  icon,
  title,
  body,
  action,
  primary = false,
}: {
  tone: 'success' | 'error';
  icon: React.ReactNode;
  title: string;
  body: string;
  action: React.ReactNode;
  primary?: boolean;
}) {
  const tones = {
    success: 'bg-success/10 text-success',
    error: 'bg-destructive/10 text-destructive',
  };
  return (
    <div className="grid gap-4 text-center">
      <div className={`mx-auto grid size-12 place-items-center rounded-full ${tones[tone]}`}>{icon}</div>
      <h1 className="text-2xl font-semibold tracking-tight">{title}</h1>
      <p className="text-muted-foreground text-sm text-balance">{body}</p>
      <Button asChild variant={primary ? 'default' : 'outline'} className="mt-2">
        {action}
      </Button>
    </div>
  );
}
