import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { z } from 'zod';
import { Loader2 } from 'lucide-react';
import { toast } from 'sonner';

import { authApi } from '@/lib/api/auth';
import { applyApiErrorToForm, type FormBannerState } from '@/lib/api/form-errors';
import { authKeys, useAuth } from '@/hooks/use-auth';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Form, FormControl, FormField, FormItem, FormLabel, FormMessage } from '@/components/ui/form';
import { PasswordInput } from '@/components/auth/password-input';
import { FormBanner } from '@/components/auth/form-banner';

// Module-level shape for type inference only; the live validation schema is built with localized
// messages inside the component (see `schema` below).
type Values = { currentPassword: string; newPassword: string; confirmPassword: string };

export function PasswordSection() {
  const { t } = useTranslation();
  const { user } = useAuth();
  // OAuth-only accounts have no password yet — show "set" (no current-password field) instead of "change".
  const hasPassword = user?.hasPassword ?? true;
  const queryClient = useQueryClient();
  const [banner, setBanner] = useState<FormBannerState | null>(null);

  const schema = useMemo(
    () =>
      z
        .object({
          currentPassword: z.string(),
          newPassword: z.string().min(8, t('profile.password.min')).max(128),
          confirmPassword: z.string().min(1, t('profile.password.confirmRequired')),
        })
        .refine((v) => v.newPassword === v.confirmPassword, {
          message: t('profile.password.mismatch'),
          path: ['confirmPassword'],
        }),
    [t],
  );

  const form = useForm<Values>({
    resolver: zodResolver(schema),
    defaultValues: { currentPassword: '', newPassword: '', confirmPassword: '' },
  });

  const change = useMutation({
    mutationFn: authApi.changePassword,
    onSuccess: () => {
      toast.success(hasPassword ? t('profile.password.changed') : t('profile.password.set'));
      form.reset();
      queryClient.invalidateQueries({ queryKey: authKeys.me }); // refresh hasPassword so this card flips to "change"
    },
    onError: (error) => setBanner(applyApiErrorToForm(error, form.setError, ['currentPassword', 'newPassword'])),
  });

  const onSubmit = form.handleSubmit((values) => {
    if (hasPassword && !values.currentPassword) {
      form.setError('currentPassword', { message: t('profile.password.enterCurrent') });
      return;
    }
    setBanner(null);
    change.mutate({ currentPassword: values.currentPassword || undefined, newPassword: values.newPassword });
  });

  return (
    <Card>
      <CardHeader>
        <CardTitle>{t('profile.sections.password')}</CardTitle>
        <CardDescription>
          {hasPassword ? t('profile.password.changeDesc') : t('profile.password.setDesc')}
        </CardDescription>
      </CardHeader>
      <CardContent>
        <FormBanner state={banner} />
        <Form {...form}>
          <form onSubmit={onSubmit} className="mt-4 grid max-w-sm gap-4">
            {hasPassword && (
              <FormField
                control={form.control}
                name="currentPassword"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>{t('profile.password.current')}</FormLabel>
                    <FormControl>
                      <PasswordInput autoComplete="current-password" {...field} />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
            )}
            <FormField
              control={form.control}
              name="newPassword"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t('profile.password.new')}</FormLabel>
                  <FormControl>
                    <PasswordInput autoComplete="new-password" {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
            <FormField
              control={form.control}
              name="confirmPassword"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t('profile.password.confirm')}</FormLabel>
                  <FormControl>
                    <PasswordInput autoComplete="new-password" {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
            <div>
              <Button type="submit" disabled={change.isPending}>
                {change.isPending && <Loader2 className="animate-spin" />}
                {hasPassword ? t('profile.password.update') : t('profile.password.setBtn')}
              </Button>
            </div>
          </form>
        </Form>
      </CardContent>
    </Card>
  );
}
