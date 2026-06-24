import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Lock, Pencil, Plus, Shield, Trash2, Users } from 'lucide-react';
import { toast } from 'sonner';

import { rolesApi, permissionsApi, PERM, type Role } from '@/lib/api/admin';
import { isApiError } from '@/lib/problem';
import { usePermission } from '@/hooks/use-permission';
import { useDocumentTitle } from '@/hooks/use-document-title';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { Skeleton } from '@/components/ui/skeleton';
import { ConfirmDialog } from '@/components/confirm-dialog';
import { EmptyState, ErrorState } from '@/components/data-states';
import { RoleEditorDialog } from '@/components/admin/role-editor-dialog';
import { meta } from './meta';

export default function RolesPage() {
  const { t } = useTranslation();
  useDocumentTitle(meta.title);
  const queryClient = useQueryClient();

  const canCreate = usePermission(PERM.rolesCreate);
  const canUpdate = usePermission(PERM.rolesUpdate);
  const canDelete = usePermission(PERM.rolesDelete);

  const roles = useQuery({ queryKey: ['admin', 'roles'], queryFn: rolesApi.list });
  const catalog = useQuery({ queryKey: ['admin', 'permissions'], queryFn: permissionsApi.catalog });

  const [editing, setEditing] = useState<Role | null>(null);
  const [editorOpen, setEditorOpen] = useState(false);
  const [deleting, setDeleting] = useState<Role | null>(null);

  const remove = useMutation({
    mutationFn: (role: Role) => rolesApi.remove(role.id),
    onSuccess: () => {
      toast.success(t('roles.deleted'));
      queryClient.invalidateQueries({ queryKey: ['admin', 'roles'] });
      setDeleting(null);
    },
    onError: (error) => toast.error(isApiError(error) ? (error.problem.detail ?? error.message) : t('roles.deleteError')),
  });

  const openCreate = () => {
    setEditing(null);
    setEditorOpen(true);
  };
  const openEdit = (role: Role) => {
    setEditing(role);
    setEditorOpen(true);
  };

  return (
    <div className="grid gap-4">
      <header>
        <h1 className="text-2xl font-semibold tracking-tight">{t('roles.title')}</h1>
        <p className="text-muted-foreground mt-1">{t('roles.subtitle')}</p>
      </header>

      <div className="flex items-center justify-between gap-3">
        <p className="text-muted-foreground text-sm">{t('roles.bundleHint')}</p>
        {canCreate && (
          <Button size="sm" onClick={openCreate} disabled={!catalog.data}>
            <Plus />
            {t('roles.newRole')}
          </Button>
        )}
      </div>

      <Card>
        <CardContent className="p-0">
          {roles.isLoading ? (
            <LoadingRows />
          ) : roles.isError ? (
            <ErrorState error={roles.error} onRetry={() => roles.refetch()} retrying={roles.isFetching} message={t('roles.loadError')} />
          ) : !roles.data || roles.data.length === 0 ? (
            <EmptyState
              icon={Shield}
              title={t('roles.emptyTitle')}
              description={t('roles.emptyDesc')}
              action={canCreate ? <Button size="sm" onClick={openCreate} disabled={!catalog.data}><Plus />{t('roles.newRole')}</Button> : undefined}
            />
          ) : (
            <ul className="divide-border divide-y">
              {roles.data.map((role) => (
                <li key={role.id} className="flex flex-wrap items-center gap-x-4 gap-y-2 p-4">
                  <div className="bg-primary/10 text-primary grid size-9 shrink-0 place-items-center rounded-lg">
                    <Shield className="size-4" />
                  </div>
                  <div className="min-w-0 flex-1">
                    <div className="flex items-center gap-2">
                      <span className="truncate font-medium">{role.name}</span>
                      {role.isSystem && (
                        <Badge variant="secondary" className="gap-1">
                          <Lock className="size-3" />
                          {t('roles.builtIn')}
                        </Badge>
                      )}
                    </div>
                    <p className="text-muted-foreground mt-0.5 flex flex-wrap items-center gap-x-3 text-sm">
                      <span>{role.permissions.includes('*') ? t('roles.allPermissions') : t('roles.permissionCount', { count: role.permissions.length })}</span>
                      <span className="inline-flex items-center gap-1">
                        <Users className="size-3.5" />
                        {t('roles.userCount', { count: role.userCount })}
                      </span>
                    </p>
                  </div>
                  <div className="flex items-center gap-1">
                    {canUpdate && (
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={() => openEdit(role)}
                        disabled={role.isSystem}
                        title={role.isSystem ? t('roles.builtInReadonly') : t('roles.editRole')}
                      >
                        <Pencil />
                        {t('actions.edit')}
                      </Button>
                    )}
                    {canDelete && !role.isSystem && (
                      <Button variant="ghost" size="icon" onClick={() => setDeleting(role)} aria-label={t('roles.deleteAria', { name: role.name })}>
                        <Trash2 className="text-destructive" />
                      </Button>
                    )}
                  </div>
                </li>
              ))}
            </ul>
          )}
        </CardContent>
      </Card>

      {catalog.data && (
        <RoleEditorDialog open={editorOpen} onOpenChange={setEditorOpen} role={editing} catalog={catalog.data} />
      )}

      <ConfirmDialog
        open={!!deleting}
        onOpenChange={(open) => !open && setDeleting(null)}
        title={t('roles.deleteTitle', { name: deleting?.name })}
        description={
          deleting && deleting.userCount > 0
            ? t('roles.deleteDescWithUsers', { count: deleting.userCount })
            : t('roles.deleteDescPlain')
        }
        confirmLabel={t('roles.deleteConfirm')}
        destructive
        pending={remove.isPending}
        onConfirm={() => deleting && remove.mutate(deleting)}
      />
    </div>
  );
}

function LoadingRows() {
  return (
    <ul className="divide-border divide-y">
      {[0, 1, 2].map((i) => (
        <li key={i} className="flex items-center gap-4 p-4">
          <Skeleton className="size-9 rounded-lg" />
          <div className="grid flex-1 gap-1.5">
            <Skeleton className="h-4 w-32" />
            <Skeleton className="h-3 w-40" />
          </div>
          <Skeleton className="h-8 w-16" />
        </li>
      ))}
    </ul>
  );
}
