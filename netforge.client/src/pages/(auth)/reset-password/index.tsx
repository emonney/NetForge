import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link, useSearchParams } from 'react-router';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useMutation } from '@tanstack/react-query';
import { z } from 'zod';
import { CircleCheck, Loader2, TriangleAlert } from 'lucide-react';

import { authApi } from '@/lib/api/auth';
import { applyApiErrorToForm, type FormBannerState } from '@/lib/api/form-errors';
import { useDocumentTitle } from '@/hooks/use-document-title';
import { Button } from '@/components/ui/button';
import { Form, FormControl, FormDescription, FormField, FormItem, FormLabel, FormMessage } from '@/components/ui/form';
import { PasswordInput } from '@/components/auth/password-input';
import { FormBanner } from '@/components/auth/form-banner';
import { meta } from './meta';

type Values = { newPassword: string; confirmPassword: string };

export default function ResetPasswordPage() {
  const { t } = useTranslation();
  useDocumentTitle(meta.title);
  const [params] = useSearchParams();
  const email = params.get('email');
  const token = params.get('token');

  const [banner, setBanner] = useState<FormBannerState | null>(null);
  const schema = useMemo(
    () =>
      z
        .object({
          newPassword: z.string().min(8, t('auth.valid.passwordMin')).max(128),
          confirmPassword: z.string().min(1, t('auth.valid.confirmRequired')),
        })
        .refine((v) => v.newPassword === v.confirmPassword, {
          message: t('auth.valid.mismatch'),
          path: ['confirmPassword'],
        }),
    [t],
  );
  const form = useForm<Values>({
    resolver: zodResolver(schema),
    defaultValues: { newPassword: '', confirmPassword: '' },
  });

  const reset = useMutation({
    mutationFn: authApi.resetPassword,
    onError: (error) => setBanner(applyApiErrorToForm(error, form.setError, ['newPassword'])),
  });

  if (!email || !token) {
    return <InvalidLink>{t('auth.reset.invalidMissing')}</InvalidLink>;
  }

  if (reset.isSuccess) {
    return (
      <div className="grid gap-4 text-center">
        <div className="bg-success/10 text-success mx-auto grid size-12 place-items-center rounded-full">
          <CircleCheck className="size-6" />
        </div>
        <h1 className="text-2xl font-semibold tracking-tight">{t('auth.reset.doneTitle')}</h1>
        <p className="text-muted-foreground text-sm">{t('auth.reset.doneDesc')}</p>
        <Button asChild className="mt-2">
          <Link to="/login">{t('auth.reset.goSignIn')}</Link>
        </Button>
      </div>
    );
  }

  const onSubmit = form.handleSubmit((values) => {
    setBanner(null);
    reset.mutate({ email, token, newPassword: values.newPassword });
  });

  return (
    <div className="grid gap-6">
      <header className="grid gap-1.5">
        <h1 className="text-2xl font-semibold tracking-tight">{t('auth.reset.title')}</h1>
        <p className="text-muted-foreground text-sm">{t('auth.reset.subtitle', { email })}</p>
      </header>

      <FormBanner state={banner} />

      <Form {...form}>
        <form onSubmit={onSubmit} className="grid gap-4" noValidate>
          <FormField
            control={form.control}
            name="newPassword"
            render={({ field }) => (
              <FormItem>
                <FormLabel>{t('auth.reset.new')}</FormLabel>
                <FormControl>
                  <PasswordInput autoComplete="new-password" autoFocus placeholder={t('auth.passwordPlaceholder')} {...field} />
                </FormControl>
                <FormDescription>{t('auth.reset.passwordHint')}</FormDescription>
                <FormMessage />
              </FormItem>
            )}
          />
          <FormField
            control={form.control}
            name="confirmPassword"
            render={({ field }) => (
              <FormItem>
                <FormLabel>{t('auth.reset.confirm')}</FormLabel>
                <FormControl>
                  <PasswordInput autoComplete="new-password" placeholder={t('auth.passwordPlaceholder')} {...field} />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />
          <Button type="submit" disabled={reset.isPending}>
            {reset.isPending && <Loader2 className="animate-spin" />}
            {t('auth.reset.submit')}
          </Button>
        </form>
      </Form>
    </div>
  );
}

function InvalidLink({ children }: { children: React.ReactNode }) {
  const { t } = useTranslation();
  return (
    <div className="grid gap-4 text-center">
      <div className="bg-destructive/10 text-destructive mx-auto grid size-12 place-items-center rounded-full">
        <TriangleAlert className="size-6" />
      </div>
      <h1 className="text-2xl font-semibold tracking-tight">{t('auth.reset.invalidTitle')}</h1>
      <p className="text-muted-foreground text-sm text-balance">{children}</p>
      <Button asChild variant="outline" className="mt-2">
        <Link to="/forgot-password">{t('auth.reset.requestNew')}</Link>
      </Button>
    </div>
  );
}
