import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { Loader2 } from 'lucide-react';
import { toast } from 'sonner';

import { rolesApi, type PermissionGroup, type Role } from '@/lib/api/admin';
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

// Expand a role's stored permissions (which may be wildcards) into the concrete set the picker shows.
function expand(permissions: string[], all: string[]): Set<string> {
  const set = new Set<string>();
  for (const p of permissions) {
    if (p === '*') all.forEach((a) => set.add(a));
    else if (p.endsWith('.*')) all.filter((a) => a.startsWith(p.slice(0, -1))).forEach((a) => set.add(a));
    else set.add(p);
  }
  return set;
}

/**
 * Create or edit a role. The permission picker is a flat, grouped list of concrete permissions; an
 * existing role's wildcards are expanded for display and the selection is saved as the concrete set
 * (equivalent grants, unambiguous to round-trip). `role === null` means create. The inner form is
 * keyed by role id so opening it for a different role remounts with fresh state — no effect sync.
 */
export function RoleEditorDialog({
  open,
  onOpenChange,
  role,
  catalog,
}: {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  role: Role | null;
  catalog: PermissionGroup[];
}) {
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-h-[90svh] gap-0 overflow-hidden p-0 sm:max-w-2xl">
        <RoleEditorForm key={role?.id ?? 'new'} role={role} catalog={catalog} onClose={() => onOpenChange(false)} />
      </DialogContent>
    </Dialog>
  );
}

function RoleEditorForm({ role, catalog, onClose }: { role: Role | null; catalog: PermissionGroup[]; onClose: () => void }) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const allNames = useMemo(() => catalog.flatMap((g) => g.permissions.map((p) => p.name)), [catalog]);

  const [name, setName] = useState(role?.name ?? '');
  const [nameError, setNameError] = useState<string | null>(null);
  const [selected, setSelected] = useState<Set<string>>(() => expand(role?.permissions ?? [], allNames));

  const save = useMutation({
    mutationFn: (body: { name: string; permissions: string[] }) =>
      role ? rolesApi.update(role.id, body) : rolesApi.create(body),
    onSuccess: () => {
      toast.success(role ? t('roles.updated') : t('roles.created'));
      queryClient.invalidateQueries({ queryKey: ['admin', 'roles'] });
      onClose();
    },
    onError: (error) => {
      if (isApiError(error) && error.fieldErrors?.name) {
        setNameError(error.fieldErrors.name[0]);
        return;
      }
      toast.error(isApiError(error) ? (error.problem.detail ?? error.message) : t('roles.saveError'));
    },
  });

  const toggle = (permission: string, on: boolean) =>
    setSelected((prev) => {
      const next = new Set(prev);
      if (on) next.add(permission);
      else next.delete(permission);
      return next;
    });

  const toggleGroup = (group: PermissionGroup, on: boolean) =>
    setSelected((prev) => {
      const next = new Set(prev);
      for (const p of group.permissions) {
        if (on) next.add(p.name);
        else next.delete(p.name);
      }
      return next;
    });

  const submit = () => {
    if (!name.trim()) {
      setNameError(t('roles.nameRequired'));
      return;
    }
    save.mutate({ name: name.trim(), permissions: [...selected] });
  };

  return (
    <>
      <DialogHeader className="border-b p-6">
        <DialogTitle>{role ? t('roles.editTitle', { name: role.name }) : t('roles.newTitle')}</DialogTitle>
        <DialogDescription>{t('roles.editorDesc')}</DialogDescription>
      </DialogHeader>

      <div className="grid max-h-[55svh] gap-5 overflow-y-auto p-6">
        <div className="grid gap-2">
          <Label htmlFor="role-name">{t('roles.name')}</Label>
          <Input
            id="role-name"
            value={name}
            onChange={(e) => {
              setName(e.target.value);
              setNameError(null);
            }}
            aria-invalid={!!nameError}
            autoFocus
          />
          {nameError && <p className="text-destructive text-sm">{nameError}</p>}
        </div>

        <fieldset className="grid gap-4">
          <legend className="text-sm font-medium">{t('nav.permissions')}</legend>
          {catalog.map((group) => {
            const groupNames = group.permissions.map((p) => p.name);
            const allOn = groupNames.every((n) => selected.has(n));
            const someOn = !allOn && groupNames.some((n) => selected.has(n));
            return (
              <div key={group.name} className="rounded-lg border">
                <label className="hover:bg-muted/40 flex cursor-pointer items-center gap-2 border-b px-3 py-2">
                  <Checkbox
                    checked={allOn ? true : someOn ? 'indeterminate' : false}
                    onCheckedChange={(v) => toggleGroup(group, v === true)}
                  />
                  <span className="font-medium capitalize">{group.name}</span>
                </label>
                <div className="grid gap-1 p-2 sm:grid-cols-2">
                  {group.permissions.map((permission) => (
                    <label
                      key={permission.name}
                      className="hover:bg-muted/40 flex cursor-pointer items-start gap-2 rounded-md p-2"
                    >
                      <Checkbox
                        checked={selected.has(permission.name)}
                        onCheckedChange={(v) => toggle(permission.name, v === true)}
                      />
                      <span className="grid gap-0.5">
                        <span className="text-sm leading-none">{permission.description}</span>
                        <code className="text-muted-foreground text-xs">{permission.name}</code>
                      </span>
                    </label>
                  ))}
                </div>
              </div>
            );
          })}
        </fieldset>
      </div>

      <DialogFooter className="border-t p-6">
        <Button variant="outline" onClick={onClose} disabled={save.isPending}>
          {t('common.cancel')}
        </Button>
        <Button onClick={submit} disabled={save.isPending}>
          {save.isPending && <Loader2 className="animate-spin" />}
          {role ? t('common.saveChanges') : t('roles.createRole')}
        </Button>
      </DialogFooter>
    </>
  );
}
