import { useTranslation } from 'react-i18next';
import { AlertCircle } from 'lucide-react';

import { Alert, AlertDescription } from '@/components/ui/alert';
import type { FormBannerState } from '@/lib/api/form-errors';

/**
 * Top-of-form error banner: plain-language message with the traceId tucked into a fold-out (never
 * raw JSON). Renders nothing when there's no banner-level message (field errors show inline).
 */
export function FormBanner({ state }: { state: FormBannerState | null }) {
  const { t } = useTranslation();
  if (!state?.message) return null;

  return (
    <Alert variant="destructive">
      <AlertCircle />
      <AlertDescription>
        <p>{state.message}</p>
        {state.traceId && (
          <details className="text-destructive/70 mt-1 text-xs">
            <summary className="cursor-pointer select-none">{t('common.technicalDetails')}</summary>
            <code className="break-all">{t('auth.trace', { id: state.traceId })}</code>
          </details>
        )}
      </AlertDescription>
    </Alert>
  );
}
