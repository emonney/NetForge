import type { ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { RotateCcw, TriangleAlert, type LucideIcon } from 'lucide-react';

import { isApiError } from '@/lib/problem';
import { cn } from '@/lib/utils';
import { Button } from '@/components/ui/button';
import { Skeleton } from '@/components/ui/skeleton';

/**
 * Designed error state (§7.0): plain-language message, a Retry, and the traceId tucked into a
 * fold-out — never raw JSON. Pass the caught error to pull its traceId automatically.
 */
export function ErrorState({
  error,
  onRetry,
  retrying = false,
  message = "We couldn't load this. Please try again.",
}: {
  error?: unknown;
  onRetry: () => void;
  retrying?: boolean;
  message?: string;
}) {
  const { t } = useTranslation();
  const traceId = isApiError(error) ? error.traceId : undefined;

  return (
    <div className="flex flex-col items-center gap-3 px-6 py-12 text-center">
      <div className="bg-destructive/10 text-destructive grid size-11 place-items-center rounded-full">
        <TriangleAlert className="size-5" />
      </div>
      <p className="text-muted-foreground max-w-sm text-sm">{message}</p>
      <Button variant="outline" size="sm" onClick={onRetry} disabled={retrying}>
        <RotateCcw />
        {t('common.retry')}
      </Button>
      {traceId && (
        <details className="text-muted-foreground/70 mt-1 text-xs">
          <summary className="cursor-pointer select-none">{t('common.technicalDetails')}</summary>
          <code className="break-all">trace: {traceId}</code>
        </details>
      )}
    </div>
  );
}

/**
 * Designed empty state (§7.0): icon + headline + helper sentence + optional primary action. Never a
 * bare "No results found."
 */
export function EmptyState({
  icon: Icon,
  title,
  description,
  action,
}: {
  icon: LucideIcon;
  title: string;
  description: string;
  action?: ReactNode;
}) {
  return (
    <div className="flex flex-col items-center gap-3 px-6 py-12 text-center">
      <div className="bg-muted text-muted-foreground grid size-12 place-items-center rounded-full">
        <Icon className="size-6" />
      </div>
      <div className="grid gap-1">
        <p className="font-medium">{title}</p>
        <p className="text-muted-foreground mx-auto max-w-sm text-sm">{description}</p>
      </div>
      {action}
    </div>
  );
}

/**
 * Designed loading state (§7.0): skeletons shaped like the content they replace — never a bare
 * spinner. `rows`/`cols` size a list or table placeholder.
 */
export function LoadingSkeleton({
  variant = 'list',
  rows = 5,
  cols = 4,
  className,
}: {
  variant?: 'list' | 'table' | 'cards';
  rows?: number;
  cols?: number;
  className?: string;
}) {
  if (variant === 'table') {
    return (
      <div className={cn('space-y-2.5', className)} aria-busy>
        {Array.from({ length: rows }).map((_, r) => (
          <div key={r} className="flex items-center gap-4">
            {Array.from({ length: cols }).map((_, c) => (
              <Skeleton key={c} className={cn('h-4 flex-1', c === 0 && 'max-w-[1.25rem]')} />
            ))}
          </div>
        ))}
      </div>
    );
  }

  if (variant === 'cards') {
    return (
      <div className={cn('grid gap-3 sm:grid-cols-2 lg:grid-cols-3', className)} aria-busy>
        {Array.from({ length: rows }).map((_, i) => (
          <Skeleton key={i} className="h-28 w-full rounded-xl" />
        ))}
      </div>
    );
  }

  return (
    <div className={cn('space-y-3', className)} aria-busy>
      {Array.from({ length: rows }).map((_, i) => (
        <div key={i} className="flex items-center gap-3">
          <Skeleton className="size-9 shrink-0 rounded-full" />
          <div className="flex-1 space-y-2">
            <Skeleton className="h-3.5 w-1/3" />
            <Skeleton className="h-3 w-2/3" />
          </div>
        </div>
      ))}
    </div>
  );
}

/** Page header: title + optional description and trailing actions. Consistent spacing app-wide. */
export function PageHeader({
  title,
  description,
  actions,
  className,
}: {
  title: ReactNode;
  description?: ReactNode;
  actions?: ReactNode;
  className?: string;
}) {
  return (
    <header className={cn('flex flex-wrap items-end justify-between gap-3', className)}>
      <div className="min-w-0">
        <h1 className="text-2xl font-semibold tracking-tight">{title}</h1>
        {description && <p className="text-muted-foreground mt-1">{description}</p>}
      </div>
      {actions && <div className="flex items-center gap-2">{actions}</div>}
    </header>
  );
}
