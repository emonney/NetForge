import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import {
  Activity,
  CheckCircle2,
  Database,
  HardDrive,
  RefreshCw,
  Server,
  ShieldAlert,
  TriangleAlert,
  XCircle,
  type LucideIcon,
} from 'lucide-react';

import { healthApi, HEALTH_PERM, type HealthStatus } from '@/lib/api/health';
import { usePermission } from '@/hooks/use-permission';
import { useDocumentTitle } from '@/hooks/use-document-title';
import { cn } from '@/lib/utils';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { Switch } from '@/components/ui/switch';
import { Label } from '@/components/ui/label';
import { BadgeCell, type BadgeTone } from '@/components/data-grid';
import { EmptyState, ErrorState, LoadingSkeleton, PageHeader } from '@/components/data-states';
import { useState } from 'react';
import { meta } from './meta';

const REFRESH_MS = 15_000;

export default function HealthPage() {
  const { t } = useTranslation();
  useDocumentTitle(meta.title);
  const canRead = usePermission(HEALTH_PERM.read);
  const [autoRefresh, setAutoRefresh] = useState(true);

  const query = useQuery({
    queryKey: ['admin', 'health'],
    queryFn: healthApi.get,
    enabled: canRead,
    refetchInterval: autoRefresh ? REFRESH_MS : false,
    refetchOnWindowFocus: true,
  });

  if (!canRead) {
    return (
      <EmptyState
        icon={ShieldAlert}
        title={t('health.noAccessTitle')}
        description={t('health.noAccessDesc')}
      />
    );
  }

  const report = query.data;

  const controls = (
    <div className="flex items-center gap-4">
      <div className="flex items-center gap-2">
        <Switch id="auto-refresh" checked={autoRefresh} onCheckedChange={setAutoRefresh} />
        <Label htmlFor="auto-refresh" className="text-muted-foreground text-sm font-normal">
          {t('health.autoRefresh')}
        </Label>
      </div>
      <Button variant="outline" size="sm" onClick={() => query.refetch()} disabled={query.isFetching}>
        <RefreshCw className={cn(query.isFetching && 'animate-spin')} />
        {t('health.refresh')}
      </Button>
    </div>
  );

  return (
    <div className="grid gap-6">
      <PageHeader
        title={t('nav.health')}
        description={t('pages.healthDesc')}
        actions={controls}
      />

      {query.isLoading ? (
        <div className="grid gap-6">
          <LoadingSkeleton variant="cards" rows={1} className="sm:grid-cols-1" />
          <LoadingSkeleton variant="cards" rows={3} />
        </div>
      ) : query.isError || !report ? (
        <div className="rounded-xl border">
          <ErrorState error={query.error} onRetry={() => query.refetch()} retrying={query.isFetching} message={t('health.loadError')} />
        </div>
      ) : (
        <>
          <OverallBanner status={report.status} checkedAt={report.checkedAt} durationMs={report.totalDurationMs} />

          {report.checks.length === 0 ? (
            <EmptyState
              icon={Activity}
              title={t('health.noChecksTitle')}
              description={t('health.noChecksDesc')}
            />
          ) : (
            <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
              {report.checks.map((check) => (
                <CheckCard key={check.name} {...check} />
              ))}
            </div>
          )}
        </>
      )}
    </div>
  );
}

function OverallBanner({ status, checkedAt, durationMs }: { status: HealthStatus; checkedAt: string; durationMs: number }) {
  const { t } = useTranslation();
  const s = STATUS[status];
  const key = status.toLowerCase();
  return (
    <Card className={cn('border-l-4', s.borderClass)}>
      <CardContent className="flex flex-wrap items-center gap-4 py-5">
        <div className={cn('grid size-12 shrink-0 place-items-center rounded-xl', s.iconWrapClass)}>
          <s.icon className="size-6" />
        </div>
        <div className="min-w-0 flex-1">
          <p className="text-lg font-semibold">{t(`health.headline.${key}`)}</p>
          <p className="text-muted-foreground text-sm">
            {t('health.lastChecked', { time: new Date(checkedAt).toLocaleTimeString(), ms: Math.round(durationMs) })}
          </p>
        </div>
        <BadgeCell label={t(`health.status.${key}`)} tone={s.tone} />
      </CardContent>
    </Card>
  );
}

function CheckCard({ name, status, description, durationMs, tags, error, data }: {
  name: string;
  status: HealthStatus;
  description: string | null;
  durationMs: number;
  tags: string[];
  error: string | null;
  data: Record<string, string>;
}) {
  const { t } = useTranslation();
  const s = STATUS[status];
  const Icon = CHECK_ICONS[name] ?? Activity;
  const entries = Object.entries(data);

  return (
    <Card className={cn('border-t-2', s.borderClass)}>
      <CardContent className="grid gap-3 py-5">
        <div className="flex items-start justify-between gap-3">
          <div className="flex min-w-0 items-center gap-2">
            <span className="text-muted-foreground"><Icon className="size-4" /></span>
            <span className="truncate font-medium">{t(`health.checks.${name}`, { defaultValue: humanize(name) })}</span>
          </div>
          <BadgeCell label={t(`health.status.${status.toLowerCase()}`)} tone={s.tone} />
        </div>

        {description && <p className="text-muted-foreground text-sm">{description}</p>}

        {entries.length > 0 && (
          <dl className="grid gap-1 text-sm">
            {entries.map(([key, value]) => (
              <div key={key} className="flex items-baseline justify-between gap-3">
                <dt className="text-muted-foreground text-xs">{t(`health.data.${key}`, { defaultValue: humanize(key) })}</dt>
                <dd className="truncate text-end font-mono text-xs">{value}</dd>
              </div>
            ))}
          </dl>
        )}

        {error && (
          <details className="text-sm">
            <summary className="text-destructive cursor-pointer select-none text-xs font-medium">{t('health.errorDetail')}</summary>
            <p className="text-muted-foreground mt-1 break-words text-xs">{error}</p>
          </details>
        )}

        <div className="text-muted-foreground flex items-center justify-between gap-2 text-xs">
          <span>{t('health.durationMs', { ms: Math.round(durationMs) })}</span>
          {tags.length > 0 && <span className="font-mono">{tags.join(' · ')}</span>}
        </div>
      </CardContent>
    </Card>
  );
}

const STATUS: Record<HealthStatus, { icon: LucideIcon; tone: BadgeTone; borderClass: string; iconWrapClass: string }> = {
  Healthy: {
    icon: CheckCircle2,
    tone: 'success',
    borderClass: 'border-l-success border-t-success',
    iconWrapClass: 'bg-success/10 text-success',
  },
  Degraded: {
    icon: TriangleAlert,
    tone: 'warning',
    borderClass: 'border-l-warning border-t-warning',
    iconWrapClass: 'bg-warning/10 text-warning',
  },
  Unhealthy: {
    icon: XCircle,
    tone: 'destructive',
    borderClass: 'border-l-destructive border-t-destructive',
    iconWrapClass: 'bg-destructive/10 text-destructive',
  },
};

const CHECK_ICONS: Record<string, LucideIcon> = {
  database: Database,
  'background-jobs': Server,
  storage: HardDrive,
};

/** "background-jobs" / "pendingMigrations" → "Background jobs" / "Pending migrations". */
function humanize(value: string): string {
  const spaced = value
    .replace(/[-_]/g, ' ')
    .replace(/([a-z])([A-Z])/g, '$1 $2')
    .trim();
  return spaced.charAt(0).toUpperCase() + spaced.slice(1).toLowerCase();
}
