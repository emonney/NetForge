import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import type { ColumnDef } from '@tanstack/react-table';
import { useNavigate } from 'react-router';
import { History, KeyRound, Lock, LockOpen, MailCheck, MailPlus, MoreHorizontal, Pencil, ShieldOff, Trash2, UserCog, UserPlus, Users as UsersIcon } from 'lucide-react';
import { toast } from 'sonner';

import { usersApi, PERM, type AdminUser } from '@/lib/api/admin';
import { isApiError } from '@/lib/problem';
import { usePermission } from '@/hooks/use-permission';
import { useDocumentTitle } from '@/hooks/use-document-title';
import { Avatar, AvatarFallback } from '@/components/ui/avatar';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import { PageHeader } from '@/components/data-states';
import { ConfirmDialog } from '@/components/confirm-dialog';
import { DataGrid, useDataGrid, selectColumn, DateCell } from '@/components/data-grid';
import { UserRolesDialog } from '@/components/admin/user-roles-dialog';
import { CreateUserDialog } from '@/components/admin/create-user-dialog';
import { EditUserDialog } from '@/components/admin/edit-user-dialog';
import { meta } from './meta';

export default function UsersPage() {
  const { t } = useTranslation();
  useDocumentTitle(meta.title);
  const queryClient = useQueryClient();
  const navigate = useNavigate();

  const canCreate = usePermission(PERM.usersCreate);
  const canUpdate = usePermission(PERM.usersUpdate);
  const canDelete = usePermission(PERM.usersDelete);
  const canReadRoles = usePermission(PERM.rolesRead);
  const canAudit = usePermission(PERM.auditRead);
  const showActions = canUpdate || canDelete;

  const grid = useDataGrid<AdminUser>({
    endpoint: '/users',
    queryKey: ['admin', 'users'],
    defaultSort: { id: 'createdAt', desc: true },
  });

  const [rolesFor, setRolesFor] = useState<AdminUser | null>(null);
  const [editing, setEditing] = useState<AdminUser | null>(null);
  const [deleting, setDeleting] = useState<AdminUser | null>(null);
  const [bulkDeleteIds, setBulkDeleteIds] = useState<string[] | null>(null);
  const [createOpen, setCreateOpen] = useState(false);

  const invalidate = () => queryClient.invalidateQueries({ queryKey: ['admin', 'users'] });
  const onError = (fallback: string) => (error: unknown) =>
    toast.error(isApiError(error) ? (error.problem.detail ?? error.message) : fallback);

  const lock = useMutation({
    mutationFn: (user: AdminUser) => (user.lockedOut ? usersApi.unlock(user.id) : usersApi.lock(user.id)),
    onSuccess: (updated) => {
      toast.success(updated.lockedOut ? t('users.locked') : t('users.unlocked'));
      invalidate();
    },
    onError: onError(t('users.lockError')),
  });

  const remove = useMutation({
    mutationFn: (user: AdminUser) => usersApi.remove(user.id),
    onSuccess: () => {
      toast.success(t('users.deleted'));
      invalidate();
      setDeleting(null);
    },
    onError: onError(t('users.deleteError')),
  });

  const confirmEmail = useMutation({
    mutationFn: (user: AdminUser) => usersApi.confirmEmail(user.id),
    onSuccess: () => {
      toast.success(t('users.verified'));
      invalidate();
    },
    onError: onError(t('users.verifyError')),
  });

  const sendReset = useMutation({
    mutationFn: (user: AdminUser) => usersApi.sendPasswordReset(user.id),
    onSuccess: () => toast.success(t('users.resetSent')),
    onError: onError(t('users.resetError')),
  });

  const resendConfirm = useMutation({
    mutationFn: (user: AdminUser) => usersApi.resendConfirmation(user.id),
    onSuccess: () => toast.success(t('users.confirmationSent')),
    onError: onError(t('users.resendError')),
  });

  const disable2fa = useMutation({
    mutationFn: (user: AdminUser) => usersApi.disableTwoFactor(user.id),
    onSuccess: () => {
      toast.success(t('users.twoFactorDisabled'));
      invalidate();
    },
    onError: onError(t('users.disable2faError')),
  });

  const bulkLock = useMutation({
    mutationFn: (ids: string[]) => Promise.all(ids.map((id) => usersApi.lock(id))),
    onSuccess: (r) => {
      toast.success(t('users.bulkLocked', { count: r.length }));
      invalidate();
    },
    onError: onError(t('users.bulkLockError')),
  });

  const bulkRemove = useMutation({
    mutationFn: (ids: string[]) => Promise.all(ids.map((id) => usersApi.remove(id))),
    onSuccess: (r) => {
      toast.success(t('users.bulkDeleted', { count: r.length }));
      invalidate();
      setBulkDeleteIds(null);
    },
    onError: onError(t('users.bulkDeleteError')),
  });

  const rowActions = (user: AdminUser) =>
    showActions && !user.isSelf ? (
      <div className="text-end">
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button variant="ghost" size="icon" className="size-8" aria-label={t('users.actionsFor', { email: user.email })}>
              <MoreHorizontal className="size-4" />
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end">
            {canUpdate && (
              <DropdownMenuItem onClick={() => setEditing(user)}>
                <Pencil />
                {t('users.editUser')}
              </DropdownMenuItem>
            )}
            {canUpdate && canReadRoles && (
              <DropdownMenuItem onClick={() => setRolesFor(user)}>
                <UserCog />
                {t('users.editRoles')}
              </DropdownMenuItem>
            )}
            {canUpdate && !user.emailConfirmed && (
              <DropdownMenuItem onClick={() => confirmEmail.mutate(user)}>
                <MailCheck />
                {t('users.verify')}
              </DropdownMenuItem>
            )}
            {canUpdate && !user.emailConfirmed && (
              <DropdownMenuItem onClick={() => resendConfirm.mutate(user)}>
                <MailPlus />
                {t('users.resendConfirm')}
              </DropdownMenuItem>
            )}
            {canUpdate && (
              <DropdownMenuItem onClick={() => sendReset.mutate(user)}>
                <KeyRound />
                {t('users.sendReset')}
              </DropdownMenuItem>
            )}
            {canUpdate && user.twoFactorEnabled && (
              <DropdownMenuItem onClick={() => disable2fa.mutate(user)}>
                <ShieldOff />
                {t('users.disable2fa')}
              </DropdownMenuItem>
            )}
            {canAudit && (
              // "AppUser" is the entity type the audit interceptor records for user rows (the CLR name);
              // it's the coordinate the /audit/entity view keys on.
              <DropdownMenuItem onClick={() => navigate(`/audit/entity/AppUser/${user.id}`)}>
                <History />
                {t('users.viewActivity')}
              </DropdownMenuItem>
            )}
            {canUpdate && (
              <DropdownMenuItem onClick={() => lock.mutate(user)}>
                {user.lockedOut ? <LockOpen /> : <Lock />}
                {user.lockedOut ? t('users.unlock') : t('users.lock')}
              </DropdownMenuItem>
            )}
            {canDelete && (
              <DropdownMenuItem variant="destructive" onClick={() => setDeleting(user)}>
                <Trash2 />
                {t('actions.delete')}
              </DropdownMenuItem>
            )}
          </DropdownMenuContent>
        </DropdownMenu>
      </div>
    ) : null;

  // Inline column defs — ids are stable so the grid's column-visibility/sort state persists.
  const columns: ColumnDef<AdminUser>[] = [
    ...(showActions ? [selectColumn<AdminUser>()] : []),
    {
      id: 'displayName',
      accessorKey: 'displayName',
      header: t('users.user'),
      meta: { label: t('users.user') },
      cell: ({ row }) => <UserIdentity user={row.original} />,
    },
    {
      id: 'roles',
      header: t('users.roles'),
      enableSorting: false,
      meta: { label: t('users.roles') },
      cell: ({ row }) => <RoleBadges roles={row.original.roles} />,
    },
    {
      id: 'status',
      header: t('fields.status'),
      enableSorting: false,
      meta: { label: t('fields.status') },
      cell: ({ row }) => <StatusBadges user={row.original} />,
    },
    {
      id: 'createdAt',
      accessorKey: 'createdAt',
      header: t('fields.joined'),
      meta: { label: t('fields.joined') },
      cell: ({ row }) => <DateCell value={row.original.createdAt} />,
    },
    ...(showActions
      ? [{ id: '__actions', header: '', enableSorting: false, enableHiding: false, meta: { label: '' }, cell: ({ row }: { row: { original: AdminUser } }) => rowActions(row.original) }]
      : []),
  ];

  return (
    <div className="grid gap-4">
      <PageHeader
        title={t('nav.users')}
        description={t('pages.usersDesc')}
        actions={
          canCreate ? (
            <Button onClick={() => setCreateOpen(true)}>
              <UserPlus className="size-4" />
              {t('users.newUser')}
            </Button>
          ) : undefined
        }
      />

      <DataGrid
        grid={grid}
        columns={columns}
        getRowId={(u) => u.id}
        searchPlaceholder={t('users.searchPlaceholder')}
        viewKey="users"
        exportable
        empty={{
          icon: UsersIcon,
          title: t('users.emptyTitle'),
          description: t('users.emptyDesc'),
        }}
        bulkActions={
          showActions
            ? (ids, clear) => {
                const targets = grid.items.filter((u) => ids.includes(u.id) && !u.isSelf).map((u) => u.id);
                return (
                  <>
                    {canUpdate && (
                      <Button
                        variant="outline"
                        size="sm"
                        disabled={targets.length === 0 || bulkLock.isPending}
                        onClick={() => {
                          bulkLock.mutate(targets);
                          clear();
                        }}
                      >
                        <Lock className="size-4" />
                        {t('users.lock')}
                      </Button>
                    )}
                    {canDelete && (
                      <Button
                        variant="outline"
                        size="sm"
                        disabled={targets.length === 0}
                        onClick={() => setBulkDeleteIds(targets)}
                      >
                        <Trash2 className="size-4" />
                        {t('actions.delete')}
                      </Button>
                    )}
                  </>
                );
              }
            : undefined
        }
      />

      <CreateUserDialog open={createOpen} onOpenChange={setCreateOpen} />

      <EditUserDialog open={!!editing} onOpenChange={(open) => !open && setEditing(null)} user={editing} />

      <UserRolesDialog open={!!rolesFor} onOpenChange={(open) => !open && setRolesFor(null)} user={rolesFor} />

      <ConfirmDialog
        open={!!deleting}
        onOpenChange={(open) => !open && setDeleting(null)}
        title={t('users.deleteTitle', { name: deleting?.displayName ?? deleting?.email })}
        description={t('users.deleteDesc')}
        confirmLabel={t('users.deleteConfirm')}
        destructive
        pending={remove.isPending}
        onConfirm={() => deleting && remove.mutate(deleting)}
      />

      <ConfirmDialog
        open={!!bulkDeleteIds}
        onOpenChange={(open) => !open && setBulkDeleteIds(null)}
        title={t('users.bulkDeleteTitle', { count: bulkDeleteIds?.length ?? 0 })}
        description={t('users.bulkDeleteDesc')}
        confirmLabel={t('users.bulkDeleteConfirm')}
        destructive
        pending={bulkRemove.isPending}
        onConfirm={() => bulkDeleteIds && bulkRemove.mutate(bulkDeleteIds)}
      />
    </div>
  );
}

function UserIdentity({ user }: { user: AdminUser }) {
  const { t } = useTranslation();
  return (
    <div className="flex items-center gap-3">
      <Avatar className="size-9">
        <AvatarFallback>{initials(user.displayName ?? user.email)}</AvatarFallback>
      </Avatar>
      <div className="min-w-0">
        <div className="flex items-center gap-2">
          <span className="truncate font-medium">{user.displayName ?? user.email}</span>
          {user.isSelf && <Badge variant="outline">{t('users.you')}</Badge>}
        </div>
        <p className="text-muted-foreground truncate text-sm">{user.email}</p>
      </div>
    </div>
  );
}

function RoleBadges({ roles }: { roles: string[] }) {
  if (roles.length === 0) return <span className="text-muted-foreground text-sm">—</span>;
  return (
    <div className="flex flex-wrap gap-1">
      {roles.map((role) => (
        <Badge key={role} variant="secondary">
          {role}
        </Badge>
      ))}
    </div>
  );
}

function StatusBadges({ user }: { user: AdminUser }) {
  const { t } = useTranslation();
  return (
    <div className="flex flex-wrap gap-1">
      {user.lockedOut && <Badge variant="destructive">{t('users.lockedBadge')}</Badge>}
      {!user.emailConfirmed && <Badge variant="secondary">{t('users.pendingEmail')}</Badge>}
      {user.twoFactorEnabled && <Badge variant="success">{t('users.twoFactor')}</Badge>}
      {user.emailConfirmed && !user.lockedOut && !user.twoFactorEnabled && (
        <span className="text-muted-foreground text-sm">{t('users.activeStatus')}</span>
      )}
    </div>
  );
}

function initials(value: string): string {
  const parts = value.trim().split(/\s+/);
  if (parts.length >= 2) return (parts[0][0] + parts[1][0]).toUpperCase();
  return value.slice(0, 2).toUpperCase();
}
