import { Navigate } from 'react-router';

import { useAuth } from '@/hooks/use-auth';
import { hasPermission } from '@/lib/permissions';
import { PERM } from '@/lib/api/admin';

// /admin has no page of its own — send the user to the first section they can open.
export default function AdminIndex() {
  const { user } = useAuth();
  const granted = user?.permissions ?? [];

  if (hasPermission(granted, PERM.usersRead)) return <Navigate to="/admin/users" replace />;
  if (hasPermission(granted, PERM.rolesRead)) return <Navigate to="/admin/roles" replace />;
  return <Navigate to="/admin/permissions" replace />;
}
