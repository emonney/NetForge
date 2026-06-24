import { Loader2 } from 'lucide-react';

import i18n from '@/i18n.config';

/** Centered spinner for the brief auth-bootstrap window before guards decide where to send you. */
export function FullScreenLoader() {
  return (
    <div className="flex min-h-svh items-center justify-center" role="status" aria-label={i18n.t('common.loading')}>
      <Loader2 className="text-muted-foreground size-6 animate-spin" />
    </div>
  );
}
