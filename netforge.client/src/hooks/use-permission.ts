import { useAuth } from '@/hooks/use-auth';
import { hasAnyPermission, hasPermission } from '@/lib/permissions';

/** True when the current user holds a permission that grants `required` (wildcards honoured). */
export function usePermission(required: string): boolean {
  const { user } = useAuth();
  return !!user && hasPermission(user.permissions, required);
}

/** True when the current user satisfies any of `required` (OR). Handy for "show if they can do
 * anything in this area" gates like the admin nav entry. */
export function useAnyPermission(required: readonly string[]): boolean {
  const { user } = useAuth();
  return !!user && hasAnyPermission(user.permissions, required);
}
