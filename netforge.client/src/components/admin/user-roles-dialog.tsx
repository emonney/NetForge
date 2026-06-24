import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Loader2 } from 'lucide-react';
import { toast } from 'sonner';

import { rolesApi, usersApi, type AdminUser } from '@/lib/api/admin';
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
import { Skeleton } from '@/components/ui/skeleton';
import { ErrorState } from '@/components/data-states';

/** Assign roles to a user. The inner form is keyed by user id so it remounts with the user's current
 * roles as initial state (no effect sync); roles are loaded lazily while the dialog is open. */
export function UserRolesDialog({
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
          <DialogTitle>{t('users.editRoles')}</DialogTitle>
          <DialogDescription>{user?.displayName ?? user?.email}</DialogDescription>
        </DialogHeader>
        {user && <UserRolesForm key={user.id} user={user} onClose={() => onOpenChange(false)} />}
      </DialogContent>
    </Dialog>
  );
}

function UserRolesForm({ user, onClose }: { user: AdminUser; onClose: () => void }) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const roles = useQuery({ queryKey: ['admin', 'roles'], queryFn: rolesApi.list });

  const [selected, setSelected] = useState<Set<string>>(() => new Set(user.roles));

  const save = useMutation({
    mutationFn: (roleNames: string[]) => usersApi.updateRoles(user.id, roleNames),
    onSuccess: () => {
      toast.success(t('users.rolesUpdated'));
      queryClient.invalidateQueries({ queryKey: ['admin', 'users'] });
      onClose();
    },
    onError: (error) => toast.error(isApiError(error) ? (error.problem.detail ?? error.message) : t('users.rolesUpdateError')),
  });

  const toggle = (name: string, on: boolean) =>
    setSelected((prev) => {
      const next = new Set(prev);
      if (on) next.add(name);
      else next.delete(name);
      return next;
    });

  return (
    <>
      {roles.isLoading ? (
        <div className="grid gap-2 py-2">
          {[0, 1, 2].map((i) => (
            <Skeleton key={i} className="h-12 w-full rounded-lg" />
          ))}
        </div>
      ) : roles.isError ? (
        <ErrorState error={roles.error} onRetry={() => roles.refetch()} retrying={roles.isFetching} message={t('users.rolesLoadError')} />
      ) : (
        <ul className="grid max-h-[50svh] gap-1 overflow-y-auto py-1">
          {roles.data?.map((role) => (
            <li key={role.id}>
              <label className="hover:bg-muted/40 flex cursor-pointer items-start gap-3 rounded-lg p-2.5">
                <Checkbox
                  checked={selected.has(role.name)}
                  onCheckedChange={(v) => toggle(role.name, v === true)}
                  className="mt-0.5"
                />
                <span className="grid gap-0.5">
                  <span className="font-medium leading-none">{role.name}</span>
                  <span className="text-muted-foreground text-xs">
                    {role.permissions.includes('*')
                      ? t('roles.allPermissions')
                      : t('roles.permissionCount', { count: role.permissions.length })}
                  </span>
                </span>
              </label>
            </li>
          ))}
        </ul>
      )}

      <DialogFooter>
        <Button variant="outline" onClick={onClose} disabled={save.isPending}>
          {t('common.cancel')}
        </Button>
        <Button onClick={() => save.mutate([...selected])} disabled={save.isPending || roles.isLoading}>
          {save.isPending && <Loader2 className="animate-spin" />}
          {t('users.saveRoles')}
        </Button>
      </DialogFooter>
    </>
  );
}
