import { useState, type FormEvent } from 'react';
import { useTranslation } from 'react-i18next';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { Loader2 } from 'lucide-react';
import { toast } from 'sonner';

import { usersApi, type AdminUser, type UpdateUserBody } from '@/lib/api/admin';
import { isApiError } from '@/lib/problem';
import { Button } from '@/components/ui/button';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';

/** Edit a user's display name and email. Keyed by user id so it remounts with the user's current values
 * as initial state (the documented derive-state-on-mount pattern — no effect sync). */
export function EditUserDialog({
  open,
  onOpenChange,
  user,
}: {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  user: AdminUser | null;
}) {
  const { t } = useTranslation();
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>{t('users.editUser')}</DialogTitle>
          <DialogDescription>{t('users.editUserDesc')}</DialogDescription>
        </DialogHeader>
        {user && <EditUserForm key={user.id} user={user} onClose={() => onOpenChange(false)} />}
      </DialogContent>
    </Dialog>
  );
}

function EditUserForm({ user, onClose }: { user: AdminUser; onClose: () => void }) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const [displayName, setDisplayName] = useState(user.displayName ?? '');
  const [email, setEmail] = useState(user.email);

  const save = useMutation({
    mutationFn: (body: UpdateUserBody) => usersApi.update(user.id, body),
    onSuccess: () => {
      toast.success(t('users.updated'));
      queryClient.invalidateQueries({ queryKey: ['admin', 'users'] });
      onClose();
    },
    onError: (error) => toast.error(isApiError(error) ? (error.problem.detail ?? error.message) : t('users.updateError')),
  });

  const submit = (e: FormEvent) => {
    e.preventDefault();
    save.mutate({ displayName: displayName.trim() || null, email: email.trim() });
  };

  return (
    <form onSubmit={submit} className="grid gap-4">
      <div className="grid gap-2">
        <Label htmlFor="eu-name">{t('users.displayNameOptional')}</Label>
        <Input id="eu-name" value={displayName} onChange={(e) => setDisplayName(e.target.value)} />
      </div>
      <div className="grid gap-2">
        <Label htmlFor="eu-email">{t('users.emailLabel')}</Label>
        <Input id="eu-email" type="email" required value={email} onChange={(e) => setEmail(e.target.value)} />
      </div>

      <DialogFooter>
        <Button type="button" variant="outline" onClick={onClose} disabled={save.isPending}>
          {t('common.cancel')}
        </Button>
        <Button type="submit" disabled={save.isPending || !email.trim()}>
          {save.isPending && <Loader2 className="animate-spin" />}
          {t('users.saveUser')}
        </Button>
      </DialogFooter>
    </form>
  );
}
