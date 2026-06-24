import { useState, type FormEvent } from 'react';
import { useTranslation } from 'react-i18next';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Loader2 } from 'lucide-react';
import { toast } from 'sonner';

import { rolesApi, usersApi, type CreateUserBody } from '@/lib/api/admin';
import { isApiError } from '@/lib/problem';
import { Button } from '@/components/ui/button';
import { Checkbox } from '@/components/ui/checkbox';
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
import { Switch } from '@/components/ui/switch';
import { Skeleton } from '@/components/ui/skeleton';

/** Provision a new user. Invite-first: with "send invitation" on, the account is created and emailed a
 * "set your password" link; turning it off reveals a temporary-password field for offline provisioning.
 * The form is mounted only while open so it always starts from defaults (no effect-based reset). */
export function CreateUserDialog({ open, onOpenChange }: { open: boolean; onOpenChange: (open: boolean) => void }) {
  const { t } = useTranslation();
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>{t('users.newUser')}</DialogTitle>
          <DialogDescription>{t('users.createDesc')}</DialogDescription>
        </DialogHeader>
        {open && <CreateUserForm onClose={() => onOpenChange(false)} />}
      </DialogContent>
    </Dialog>
  );
}

function CreateUserForm({ onClose }: { onClose: () => void }) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const roles = useQuery({ queryKey: ['admin', 'roles'], queryFn: rolesApi.list });

  const [email, setEmail] = useState('');
  const [displayName, setDisplayName] = useState('');
  const [selectedRoles, setSelectedRoles] = useState<Set<string>>(() => new Set());
  const [emailConfirmed, setEmailConfirmed] = useState(true);
  const [sendInvite, setSendInvite] = useState(true);
  const [password, setPassword] = useState('');

  const create = useMutation({
    mutationFn: (body: CreateUserBody) => usersApi.create(body),
    onSuccess: (_user, body) => {
      toast.success(body.sendInvite && !body.password ? t('users.invitedToast') : t('users.createdToast'));
      queryClient.invalidateQueries({ queryKey: ['admin', 'users'] });
      onClose();
    },
    onError: (error) => toast.error(isApiError(error) ? (error.problem.detail ?? error.message) : t('users.createError')),
  });

  const toggleRole = (name: string, on: boolean) =>
    setSelectedRoles((prev) => {
      const next = new Set(prev);
      if (on) next.add(name);
      else next.delete(name);
      return next;
    });

  const submit = (e: FormEvent) => {
    e.preventDefault();
    create.mutate({
      email: email.trim(),
      displayName: displayName.trim() || null,
      roles: [...selectedRoles],
      emailConfirmed,
      sendInvite,
      password: sendInvite ? null : password || null,
    });
  };

  return (
    <form onSubmit={submit} className="grid gap-4">
      <div className="grid gap-2">
        <Label htmlFor="cu-email">{t('users.emailLabel')}</Label>
        <Input id="cu-email" type="email" required autoFocus value={email} onChange={(e) => setEmail(e.target.value)} />
      </div>
      <div className="grid gap-2">
        <Label htmlFor="cu-name">{t('users.displayNameOptional')}</Label>
        <Input id="cu-name" value={displayName} onChange={(e) => setDisplayName(e.target.value)} />
      </div>

      <div className="grid gap-1.5">
        <Label>{t('users.rolesOptional')}</Label>
        {roles.isLoading ? (
          <Skeleton className="h-20 w-full rounded-lg" />
        ) : (
          <ul className="max-h-40 overflow-y-auto rounded-lg border p-1">
            {roles.data?.map((role) => (
              <li key={role.id}>
                <label className="hover:bg-muted/40 flex cursor-pointer items-center gap-2 rounded-md p-2 text-sm">
                  <Checkbox checked={selectedRoles.has(role.name)} onCheckedChange={(v) => toggleRole(role.name, v === true)} />
                  {role.name}
                </label>
              </li>
            ))}
          </ul>
        )}
        <p className="text-muted-foreground text-xs">{t('users.rolesHint')}</p>
      </div>

      <label className="flex items-center justify-between gap-3">
        <span className="text-sm">{t('users.markVerified')}</span>
        <Switch checked={emailConfirmed} onCheckedChange={setEmailConfirmed} />
      </label>
      <label className="flex items-center justify-between gap-3">
        <span className="text-sm">{t('users.sendInvite')}</span>
        <Switch checked={sendInvite} onCheckedChange={setSendInvite} />
      </label>

      {!sendInvite && (
        <div className="grid gap-2">
          <Label htmlFor="cu-pass">{t('users.tempPassword')}</Label>
          <Input id="cu-pass" type="text" value={password} onChange={(e) => setPassword(e.target.value)} autoComplete="new-password" />
          <p className="text-muted-foreground text-xs">{t('users.tempPasswordHint')}</p>
        </div>
      )}

      <DialogFooter>
        <Button type="button" variant="outline" onClick={onClose} disabled={create.isPending}>
          {t('common.cancel')}
        </Button>
        <Button type="submit" disabled={create.isPending || !email.trim()}>
          {create.isPending && <Loader2 className="animate-spin" />}
          {t('users.create')}
        </Button>
      </DialogFooter>
    </form>
  );
}
