import { Outlet } from 'react-router';
import { useTranslation } from 'react-i18next';
import { ShieldAlert } from 'lucide-react';

import { useAuth } from '@/hooks/use-auth';
import { hasAnyPermission } from '@/lib/permissions';
import { PERM } from '@/lib/api/admin';
import { HEALTH_PERM } from '@/lib/api/health';
import { EmptyState } from '@/components/data-states';

// Permission gate for the whole /admin subtree. Navigation between admin sections is handled by the
// app sidebar; if the user can open none of them, they get a designed "no access" state rather than a
// blank or broken page. The API enforces every action regardless.
export default function AdminLayout() {
  const { t } = useTranslation();
  const { user } = useAuth();
  const canEnter = hasAnyPermission(user?.permissions ?? [], [
    PERM.usersRead,
    PERM.rolesRead,
    PERM.settingsRead,
    PERM.auditRead,
    PERM.webhooksRead,
    HEALTH_PERM.read,
  ]);

  if (!canEnter) {
    return (
      <EmptyState
        icon={ShieldAlert}
        title={t('admin.noAccessTitle')}
        description={t('admin.noAccessDesc')}
      />
    );
  }

  return <Outlet />;
}
