import { useMemo, useState, type ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { Link, useNavigate, useSearchParams } from 'react-router';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useMutation } from '@tanstack/react-query';
import { z } from 'zod';
import { Loader2, FlaskConical } from 'lucide-react';

import { authApi } from '@/lib/api/auth';
import { applyApiErrorToForm, type FormBannerState } from '@/lib/api/form-errors';
import { isApiError } from '@/lib/problem';
import { useSetCurrentUser } from '@/hooks/use-auth';
import { useDocumentTitle } from '@/hooks/use-document-title';
import { usePublicConfig } from '@/hooks/use-public-config';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Checkbox } from '@/components/ui/checkbox';
import { Form, FormControl, FormField, FormItem, FormLabel, FormMessage } from '@/components/ui/form';
import { PasswordInput } from '@/components/auth/password-input';
import { FormBanner } from '@/components/auth/form-banner';
import { meta } from './meta';

type Values = { email: string; password: string; rememberMe: boolean };

export default function LoginPage() {
  const { t } = useTranslation();
  useDocumentTitle(meta.title);
  const navigate = useNavigate();
  const [params] = useSearchParams();
  const returnUrl = safeReturn(params.get('returnUrl'));
  const setCurrentUser = useSetCurrentUser();

  const schema = useMemo(
    () =>
      z.object({
        email: z.string().min(1, t('auth.valid.emailRequired')).email(t('auth.valid.emailInvalid')),
        password: z.string().min(1, t('auth.valid.passwordRequired')),
        rememberMe: z.boolean(),
      }),
    [t],
  );

  const form = useForm<Values>({
    resolver: zodResolver(schema),
    defaultValues: { email: '', password: '', rememberMe: true },
  });

  const { data: publicConfig } = usePublicConfig();
  const oauthError = params.get('error');
  const [banner, setBanner] = useState<FormBannerState | null>(
    oauthError ? { message: t(`auth.oauthError.${oauthError}`, { defaultValue: t('auth.oauthError.generic') }) } : null,
  );

  const login = useMutation({
    mutationFn: authApi.login,
    onSuccess: (result) => {
      if (result.user) {
        setCurrentUser(result.user);
        navigate(returnUrl, { replace: true });
      }
    },
    onError: (error) => setBanner(applyApiErrorToForm(error, form.setError, ['email', 'password'])),
  });

  const resend = useMutation({ mutationFn: authApi.resendConfirmation });
  const needsConfirmation = isApiError(login.error) && login.error.code === 'EMAIL_NOT_CONFIRMED';

  const onSubmit = form.handleSubmit((values) => {
    setBanner(null);
    login.mutate(values);
  });

  // Optional OAuth sign-in buttons — built in TS into a const slot so the JSX below needs no build-time conditional.
  const slots: Record<string, ReactNode> = {};

  return (
    <div className="grid gap-6">
      <header className="grid gap-1.5">
        <h1 className="text-2xl font-semibold tracking-tight">{t('auth.login.title')}</h1>
        <p className="text-muted-foreground text-sm">{t('auth.login.subtitle')}</p>
      </header>

      {publicConfig?.demoLogin && (
        <div className="bg-muted/40 rounded-lg border px-3.5 py-3 text-sm">
          <div className="text-foreground flex items-center gap-2 font-medium">
            <FlaskConical className="size-4" />
            {t('auth.login.demoTitle')}
          </div>
          <button
            type="button"
            onClick={() => {
              form.setValue('email', publicConfig?.demoLogin?.email ?? '', { shouldValidate: true });
              form.setValue('password', publicConfig?.demoLogin?.password ?? '', { shouldValidate: true });
            }}
            className="bg-background hover:border-primary/50 mt-2 inline-flex items-center gap-2 rounded-md border px-2.5 py-1.5 font-mono text-xs transition-colors"
          >
            <span>{publicConfig.demoLogin.email}</span>
            <span className="text-muted-foreground">/</span>
            <span>{publicConfig.demoLogin.password}</span>
          </button>
          <p className="text-muted-foreground mt-2 text-xs">{t('auth.login.demoHint')}</p>
        </div>
      )}

      <FormBanner state={banner} />

      {needsConfirmation && (
        <Button
          type="button"
          variant="outline"
          size="sm"
          disabled={resend.isPending || resend.isSuccess}
          onClick={() => resend.mutate({ email: form.getValues('email') })}
        >
          {resend.isSuccess ? t('auth.login.resendSent') : t('auth.login.resend')}
        </Button>
      )}

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

          <FormField
            control={form.control}
            name="password"
            render={({ field }) => (
              <FormItem>
                <div className="flex items-center justify-between">
                  <FormLabel>{t('auth.password')}</FormLabel>
                  <Link to="/forgot-password" className="text-muted-foreground hover:text-foreground text-sm">
                    {t('auth.login.forgot')}
                  </Link>
                </div>
                <FormControl>
                  <PasswordInput autoComplete="current-password" placeholder={t('auth.passwordPlaceholder')} {...field} />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />

          <FormField
            control={form.control}
            name="rememberMe"
            render={({ field }) => (
              <FormItem className="flex items-center gap-2">
                <FormControl>
                  <Checkbox checked={field.value} onCheckedChange={field.onChange} id="rememberMe" />
                </FormControl>
                <FormLabel htmlFor="rememberMe" className="font-normal">
                  {t('auth.login.keepSignedIn')}
                </FormLabel>
              </FormItem>
            )}
          />

          <Button type="submit" disabled={login.isPending}>
            {login.isPending && <Loader2 className="animate-spin" />}
            {t('auth.login.signIn')}
          </Button>
        </form>
      </Form>

      {slots.oauthButtons}

      {publicConfig?.allowRegistration !== false && (
        <p className="text-muted-foreground text-center text-sm">
          {t('auth.login.noAccount')}{' '}
          <Link to="/register" className="text-foreground font-medium hover:underline">
            {t('auth.login.createOne')}
          </Link>
        </p>
      )}
    </div>
  );
}

function safeReturn(value: string | null): string {
  return value && value.startsWith('/') && !value.startsWith('//') ? value : '/';
}
