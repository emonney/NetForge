import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useMutation } from '@tanstack/react-query';
import { z } from 'zod';
import { ArrowLeft, Loader2, MailCheck } from 'lucide-react';

import { authApi } from '@/lib/api/auth';
import { applyApiErrorToForm, type FormBannerState } from '@/lib/api/form-errors';
import { useDocumentTitle } from '@/hooks/use-document-title';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Form, FormControl, FormField, FormItem, FormLabel, FormMessage } from '@/components/ui/form';
import { FormBanner } from '@/components/auth/form-banner';
import { meta } from './meta';

type Values = { email: string };

export default function ForgotPasswordPage() {
  const { t } = useTranslation();
  useDocumentTitle(meta.title);
  const [banner, setBanner] = useState<FormBannerState | null>(null);
  const schema = useMemo(
    () => z.object({ email: z.string().min(1, t('auth.valid.emailRequired')).email(t('auth.valid.emailInvalid')) }),
    [t],
  );
  const form = useForm<Values>({ resolver: zodResolver(schema), defaultValues: { email: '' } });

  const forgot = useMutation({
    mutationFn: authApi.forgotPassword,
    onError: (error) => setBanner(applyApiErrorToForm(error, form.setError, ['email'])),
  });

  const onSubmit = form.handleSubmit((values) => {
    setBanner(null);
    forgot.mutate(values);
  });

  if (forgot.isSuccess) {
    return (
      <div className="grid gap-4 text-center">
        <div className="bg-success/10 text-success mx-auto grid size-12 place-items-center rounded-full">
          <MailCheck className="size-6" />
        </div>
        <h1 className="text-2xl font-semibold tracking-tight">{t('auth.forgot.sentTitle')}</h1>
        <p className="text-muted-foreground text-sm text-balance">
          {t('auth.forgot.sentDesc', { email: form.getValues('email') })}
        </p>
        <Button asChild variant="outline" className="mt-2">
          <Link to="/login">{t('auth.backToSignIn')}</Link>
        </Button>
      </div>
    );
  }

  return (
    <div className="grid gap-6">
      <header className="grid gap-1.5">
        <h1 className="text-2xl font-semibold tracking-tight">{t('auth.forgot.title')}</h1>
        <p className="text-muted-foreground text-sm">{t('auth.forgot.subtitle')}</p>
      </header>

      <FormBanner state={banner} />

      <Form {...form}>
        <form onSubmit={onSubmit} className="grid gap-4" noValidate>
          <FormField
            control={form.control}
            name="email"
            render={({ field }) => (
              <FormItem>
                <FormLabel>{t('auth.email')}</FormLabel>
                <FormControl>
                  <Input type="email" autoComplete="email" autoFocus placeholder={t('auth.emailPlaceholder')} {...field} />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />
          <Button type="submit" disabled={forgot.isPending}>
            {forgot.isPending && <Loader2 className="animate-spin" />}
            {t('auth.forgot.submit')}
          </Button>
        </form>
      </Form>

      <Link to="/login" className="text-muted-foreground hover:text-foreground inline-flex items-center justify-center gap-1.5 text-sm">
        <ArrowLeft className="size-4" />
        {t('auth.backToSignIn')}
      </Link>
    </div>
  );
}
