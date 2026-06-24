import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Loader2, SlidersHorizontal } from 'lucide-react';
import { toast } from 'sonner';

import { settingsApi, PERM, type Setting, type SettingValue } from '@/lib/api/admin';
import { isApiError } from '@/lib/problem';
import { usePermission } from '@/hooks/use-permission';
import { useDocumentTitle } from '@/hooks/use-document-title';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Skeleton } from '@/components/ui/skeleton';
import { Switch } from '@/components/ui/switch';
import { EmptyState, ErrorState } from '@/components/data-states';
import { SectionLayout } from '@/components/section-layout';
import { meta } from './meta';

const settingsKey = ['admin', 'settings'];

export default function SettingsPage() {
  const { t } = useTranslation();
  useDocumentTitle(meta.title);
  const queryClient = useQueryClient();
  const canUpdate = usePermission(PERM.settingsUpdate);

  const { data, isLoading, isError, error, refetch, isFetching } = useQuery({
    queryKey: settingsKey,
    queryFn: settingsApi.list,
  });

  const save = useMutation({
    mutationFn: ({ key, value }: { key: string; value: SettingValue }) => settingsApi.update(key, value),
    onSuccess: () => {
      toast.success(t('settings.saved'));
      queryClient.invalidateQueries({ queryKey: settingsKey });
    },
    onError: (e) => toast.error(isApiError(e) ? (e.problem.detail ?? e.message) : t('settings.saveError')),
  });

  return (
    <div className="grid gap-4">
      <header>
        <h1 className="text-2xl font-semibold tracking-tight">{t('settings.title')}</h1>
        <p className="text-muted-foreground mt-1">{t('settings.subtitle')}</p>
      </header>

      {isLoading ? (
        <LoadingState />
      ) : isError ? (
        <Card>
          <ErrorState error={error} onRetry={() => refetch()} retrying={isFetching} message={t('settings.loadError')} />
        </Card>
      ) : !data || data.length === 0 ? (
        <Card>
          <EmptyState icon={SlidersHorizontal} title={t('settings.emptyTitle')} description={t('settings.emptyDesc')} />
        </Card>
      ) : (
        <SectionLayout
          side="end"
          sections={data.map((group) => ({
            id: group.category,
            label: t(`settings.categories.${group.category.toLowerCase()}`, { defaultValue: group.category }),
            content: (
              <Card>
                <CardHeader>
                  <CardTitle className="capitalize">{t(`settings.categories.${group.category.toLowerCase()}`, { defaultValue: group.category })}</CardTitle>
                </CardHeader>
                <CardContent>
                  <ul className="divide-border divide-y">
                    {group.settings.map((setting) => (
                      <li key={setting.key}>
                        <SettingRow
                          key={`${setting.key}:${String(setting.value)}`}
                          setting={setting}
                          canUpdate={canUpdate}
                          saving={save.isPending && save.variables?.key === setting.key}
                          onSave={(value) => save.mutate({ key: setting.key, value })}
                        />
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

function SettingRow({
  setting,
  canUpdate,
  saving,
  onSave,
}: {
  setting: Setting;
  canUpdate: boolean;
  saving: boolean;
  onSave: (value: SettingValue) => void;
}) {
  const label = humanize(setting.key);

  return (
    <div className="flex flex-col gap-2 py-4 first:pt-0 last:pb-0 sm:flex-row sm:items-center sm:justify-between">
      <div className="min-w-0">
        <p className="font-medium">{label}</p>
        <code className="text-muted-foreground text-xs">{setting.key}</code>
      </div>
      {setting.kind === 'boolean' ? (
        <Switch
          checked={setting.value as boolean}
          disabled={!canUpdate || saving}
          onCheckedChange={(value) => onSave(value)}
          aria-label={label}
        />
      ) : setting.kind === 'choice' ? (
        <ChoiceSetting setting={setting} canUpdate={canUpdate} saving={saving} onSave={onSave} />
      ) : (
        <TextSetting setting={setting} canUpdate={canUpdate} saving={saving} onSave={onSave} />
      )}
    </div>
  );
}

function TextSetting({
  setting,
  canUpdate,
  saving,
  onSave,
}: {
  setting: Setting;
  canUpdate: boolean;
  saving: boolean;
  onSave: (value: SettingValue) => void;
}) {
  const { t } = useTranslation();
  const [draft, setDraft] = useState(String(setting.value));
  const dirty = draft !== String(setting.value);

  const commit = () => {
    if (!dirty) return;
    onSave(setting.kind === 'number' ? Number(draft) : draft);
  };

  return (
    <div className="flex items-center gap-2 sm:w-72">
      <Input
        type={setting.kind === 'number' ? 'number' : 'text'}
        value={draft}
        onChange={(e) => setDraft(e.target.value)}
        disabled={!canUpdate || saving}
        className="flex-1"
      />
      <Button size="sm" variant="outline" onClick={commit} disabled={!canUpdate || !dirty || saving}>
        {saving && <Loader2 className="animate-spin" />}
        {t('common.save')}
      </Button>
    </div>
  );
}

function ChoiceSetting({
  setting,
  canUpdate,
  saving,
  onSave,
}: {
  setting: Setting;
  canUpdate: boolean;
  saving: boolean;
  onSave: (value: SettingValue) => void;
}) {
  const options = setting.options ?? [];
  const current = String(setting.value);
  const hasCurrent = options.some((o) => o.value === current);

  return (
    <div className="flex items-center gap-2 sm:w-72">
      <select
        value={current}
        disabled={!canUpdate || saving}
        onChange={(e) => e.target.value !== current && onSave(e.target.value)}
        className="border-input bg-background ring-offset-background focus-visible:ring-ring h-9 flex-1 rounded-md border px-3 text-sm capitalize focus-visible:ring-2 focus-visible:outline-none disabled:cursor-not-allowed disabled:opacity-50"
      >
        {!hasCurrent && current && <option value={current}>{current}</option>}
        {options.map((o) => (
          <option key={o.value} value={o.value}>
            {o.label}
          </option>
        ))}
      </select>
      {saving && <Loader2 className="text-muted-foreground size-4 animate-spin" />}
    </div>
  );
}

// "Account.AllowRegistration" → "Allow registration"
function humanize(key: string): string {
  const last = key.includes('.') ? key.slice(key.lastIndexOf('.') + 1) : key;
  const spaced = last.replace(/([a-z0-9])([A-Z])/g, '$1 $2');
  return spaced.charAt(0).toUpperCase() + spaced.slice(1).toLowerCase();
}

function LoadingState() {
  return (
    <Card>
      <CardHeader>
        <Skeleton className="h-5 w-28" />
      </CardHeader>
      <CardContent className="grid gap-4">
        {[0, 1].map((i) => (
          <div key={i} className="flex items-center justify-between">
            <div className="grid gap-1.5">
              <Skeleton className="h-4 w-40" />
              <Skeleton className="h-3 w-32" />
            </div>
            <Skeleton className="h-6 w-10 rounded-full" />
          </div>
        ))}
      </CardContent>
    </Card>
  );
}
