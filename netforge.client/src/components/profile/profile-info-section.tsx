import { type ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { BadgeCheck, Loader2 } from 'lucide-react';

import { authApi } from '@/lib/api/auth';
import { useAuth, useSetCurrentUser } from '@/hooks/use-auth';
import { UserAvatar } from '@/components/user-avatar';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Form } from '@/components/ui/form';
import { FormBanner } from '@/components/auth/form-banner';
import { Field, useSubmitForm } from '@/components/forms';

const schema = z.object({ displayName: z.string().max(100) });
type Values = z.infer<typeof schema>;

export function ProfileInfoSection() {
  const { t } = useTranslation();
  const { user } = useAuth();
  const setCurrentUser = useSetCurrentUser();

  const form = useForm<Values>({
    resolver: zodResolver(schema),
    values: { displayName: user?.displayName ?? '' },
  });

  const { submit, isPending, banner } = useSubmitForm({
    form,
    mutationFn: authApi.updateProfile,
    fields: ['displayName'],
    successMessage: t('profile.info.updated'),
    onSuccess: setCurrentUser,
    transform: (values) => ({ displayName: values.displayName.trim() || null }),
  });

  if (!user) return null;

  // Uploadable avatar in editions with FileUploads; a static initials avatar otherwise. Built in TS so
  // the JSX below needs no build-time conditional.
  const slots: Record<string, ReactNode> = {
    avatarBlock: (
      <UserAvatar
        name={user.displayName ?? user.email}
        avatarUrl={user.avatarUrl}
        className="size-16"
        fallbackClassName="text-lg"
      />
    ),
  };

  return (
    <Card>
      <CardHeader>
        <CardTitle>{t('profile.sections.profile')}</CardTitle>
        <CardDescription>{t('profile.info.desc')}</CardDescription>
      </CardHeader>
      <CardContent className="grid gap-6">
        <div className="flex flex-wrap items-center gap-4">
          {slots.avatarBlock}
          <div className="grid gap-1">
            <div className="flex items-center gap-2">
              <span className="font-medium">{user.email}</span>
              {user.emailConfirmed ? (
                <Badge variant="success">
                  <BadgeCheck />
                  {t('profile.info.verified')}
                </Badge>
              ) : (
                <Badge variant="secondary">{t('profile.info.unverified')}</Badge>
              )}
            </div>
            <p className="text-muted-foreground text-sm">{t('profile.info.emailIdentity')}</p>
          </div>
        </div>

        <FormBanner state={banner} />

        <Form {...form}>
          <form onSubmit={submit} className="grid max-w-sm gap-4">
            <Field<Values> name="displayName" label={t('profile.info.displayName')} placeholder={t('profile.info.namePlaceholder')} />
            <div>
              <Button type="submit" disabled={isPending || !form.formState.isDirty}>
                {isPending && <Loader2 className="animate-spin" />}
                {t('common.saveChanges')}
              </Button>
            </div>
          </form>
        </Form>
      </CardContent>
    </Card>
  );
}
