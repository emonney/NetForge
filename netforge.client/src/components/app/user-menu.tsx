import { type ReactNode } from 'react';
import { useNavigate } from 'react-router';
import { useTranslation } from 'react-i18next';
import { LogOut, Shield, User } from 'lucide-react';

import { useAuth, useLogout } from '@/hooks/use-auth';
import { hasPermission } from '@/lib/permissions';
import { PERM } from '@/lib/api/admin';
import { UserAvatar } from '@/components/user-avatar';
import { Button } from '@/components/ui/button';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';

export function UserMenu() {
  const { user } = useAuth();
  const { t } = useTranslation();
  const navigate = useNavigate();
  const logout = useLogout();

  if (!user) return null;

  const signOut = () => logout.mutate(undefined, { onSettled: () => navigate('/login', { replace: true }) });

  // Land on the first admin area the user can actually open; hidden entirely if they can open none.
  const adminHref = hasPermission(user.permissions, PERM.usersRead)
    ? '/admin/users'
    : hasPermission(user.permissions, PERM.rolesRead)
      ? '/admin/roles'
      : hasPermission(user.permissions, PERM.settingsRead)
        ? '/admin/settings'
        : null;

  // Optional account-menu item — built in TS into a const slot so the menu JSX needs no build-time conditional.
  const slots: Record<string, ReactNode> = {};

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button variant="ghost" size="icon" className="rounded-full" aria-label={t('account.menuLabel')}>
          <UserAvatar name={user.displayName ?? user.email} avatarUrl={user.avatarUrl} className="size-8" />
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end" className="w-56">
        <DropdownMenuLabel className="grid gap-0.5">
          <span className="truncate font-medium">{user.displayName ?? t('account.menuLabel')}</span>
          <span className="text-muted-foreground truncate text-xs font-normal">{user.email}</span>
        </DropdownMenuLabel>
        <DropdownMenuSeparator />
        <DropdownMenuItem onClick={() => navigate('/profile')}>
          <User />
          {t('account.profile')}
        </DropdownMenuItem>
        {slots.whatsNewItem}
        {slots.tourItem}
        {adminHref && (
          <DropdownMenuItem onClick={() => navigate(adminHref)}>
            <Shield />
            {t('account.administration')}
          </DropdownMenuItem>
        )}
        {slots.tenantSection}
        <DropdownMenuSeparator />
        <DropdownMenuItem onClick={signOut} disabled={logout.isPending}>
          <LogOut />
          {t('account.signOut')}
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
