import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { KeyRound } from 'lucide-react';

import { permissionsApi } from '@/lib/api/admin';
import { useDocumentTitle } from '@/hooks/use-document-title';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Skeleton } from '@/components/ui/skeleton';
import { EmptyState, ErrorState } from '@/components/data-states';
import { SectionLayout } from '@/components/section-layout';
import { meta } from './meta';

export default function PermissionsPage() {
  const { t } = useTranslation();
  useDocumentTitle(meta.title);

  const { data, isLoading, isError, error, refetch, isFetching } = useQuery({
    queryKey: ['admin', 'permissions'],
    queryFn: permissionsApi.catalog,
  });

  return (
    <div className="grid gap-4">
      <header>
        <h1 className="text-2xl font-semibold tracking-tight">{t('permissions.title')}</h1>
        <p className="text-muted-foreground mt-1">{t('permissions.subtitle')}</p>
      </header>

      {isLoading ? (
        <LoadingState />
      ) : isError ? (
        <Card>
          <ErrorState error={error} onRetry={() => refetch()} retrying={isFetching} message={t('permissions.loadError')} />
        </Card>
      ) : !data || data.length === 0 ? (
        <Card>
          <EmptyState icon={KeyRound} title={t('permissions.emptyTitle')} description={t('permissions.emptyDesc')} />
        </Card>
      ) : (
        <SectionLayout
          side="end"
          sections={data.map((group) => ({
            id: group.name,
            label: t(`permissions.groups.${group.name}`, { defaultValue: group.name }),
            badge: group.permissions.length,
            content: (
              <Card>
                <CardHeader>
                  <CardTitle className="capitalize">{t(`permissions.groups.${group.name}`, { defaultValue: group.name })}</CardTitle>
                </CardHeader>
                <CardContent>
                  <ul className="divide-border divide-y">
                    {group.permissions.map((permission) => (
                      <li key={permission.name} className="flex flex-col gap-1 py-3 first:pt-0 last:pb-0 sm:flex-row sm:items-center sm:justify-between">
                        <span className="text-foreground">{permission.description}</span>
                        <code className="bg-muted text-muted-foreground w-fit rounded px-1.5 py-0.5 text-xs">{permission.name}</code>
                      </li>
                    ))}
                  </ul>
                </CardContent>
              </Card>
            ),
          }))}
        />
      )}
    </div>
  );
}

function LoadingState() {
  return (
    <div className="grid gap-4">
      {[0, 1].map((card) => (
        <Card key={card}>
          <CardHeader>
            <Skeleton className="h-5 w-28" />
          </CardHeader>
          <CardContent className="grid gap-3">
            {[0, 1, 2].map((row) => (
              <div key={row} className="flex items-center justify-between">
                <Skeleton className="h-4 w-48" />
                <Skeleton className="h-4 w-24" />
              </div>
            ))}
          </CardContent>
        </Card>
      ))}
    </div>
  );
}
