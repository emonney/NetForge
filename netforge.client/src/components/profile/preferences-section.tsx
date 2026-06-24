import { useMutation } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { Loader2 } from 'lucide-react';
import { toast } from 'sonner';

import { authApi } from '@/lib/api/auth';
import { isApiError } from '@/lib/problem';
import { useAuth, useSetCurrentUser } from '@/hooks/use-auth';
import { LANGUAGES } from '@/i18n.config';
import { cn } from '@/lib/utils';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Label } from '@/components/ui/label';

// IANA zone list straight from the platform; falls back to the user's current zone if unavailable.
const TIME_ZONES: string[] =
  typeof Intl.supportedValuesOf === 'function'
    ? Intl.supportedValuesOf('timeZone')
    : [Intl.DateTimeFormat().resolvedOptions().timeZone];

const selectClass =
  'border-input bg-transparent focus-visible:border-ring focus-visible:ring-ring/50 h-9 w-full rounded-md border px-3 text-sm shadow-xs outline-none focus-visible:ring-[3px] disabled:opacity-50';

/**
 * User-scoped preferences (language + timezone), persisted on the account so they follow the user
 * across devices. Language also drives i18n immediately. App/Tenant settings live in /admin/settings.
 */
export function PreferencesSection() {
  const { user } = useAuth();
  const { t, i18n } = useTranslation();
  const setUser = useSetCurrentUser();

  const save = useMutation({
    mutationFn: authApi.updatePreferences,
    onSuccess: (updated) => {
      setUser(updated);
      toast.success(t('profile.preferences.savedToast'));
    },
    onError: (e) => toast.error(isApiError(e) ? (e.problem.detail ?? e.message) : t('profile.preferences.saveError')),
  });

  const currentLanguage = user?.locale ?? i18n.resolvedLanguage ?? 'en';
  const currentZone = user?.timeZone ?? Intl.DateTimeFormat().resolvedOptions().timeZone;

  const onLanguage = (code: string) => {
    i18n.changeLanguage(code);
    save.mutate({ locale: code });
  };

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2">
          {t('profile.sections.preferences')}
          {save.isPending && <Loader2 className="text-muted-foreground size-4 animate-spin" />}
        </CardTitle>
        <CardDescription>{t('profile.preferences.desc')}</CardDescription>
      </CardHeader>
      <CardContent className="grid max-w-md gap-5">
        <div className="grid gap-2">
          <Label htmlFor="pref-language">{t('language.label')}</Label>
          <select
            id="pref-language"
            className={cn(selectClass)}
            value={currentLanguage}
            disabled={save.isPending}
            onChange={(e) => onLanguage(e.target.value)}
          >
            {LANGUAGES.map((language) => (
              <option key={language.code} value={language.code}>
                {language.name}
              </option>
            ))}
          </select>
        </div>

        <div className="grid gap-2">
          <Label htmlFor="pref-timezone">{t('profile.preferences.timezone')}</Label>
          <select
            id="pref-timezone"
            className={cn(selectClass)}
            value={currentZone}
            disabled={save.isPending}
            onChange={(e) => save.mutate({ timeZone: e.target.value })}
          >
            {TIME_ZONES.map((zone) => (
              <option key={zone} value={zone}>
                {zone}
              </option>
            ))}
          </select>
        </div>
      </CardContent>
    </Card>
  );
}
