import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { authApi, type AuthUser } from '@/lib/api/auth';
import { isApiError } from '@/lib/problem';

export const authKeys = { me: ['auth', 'me'] as const };

async function fetchMe(): Promise<AuthUser | null> {
  try {
    return await authApi.me();
  } catch (error) {
    // 401 just means "not signed in" — a null user, not an error to surface.
    if (isApiError(error) && error.status === 401) return null;
    throw error;
  }
}

export function useCurrentUser() {
  return useQuery({ queryKey: authKeys.me, queryFn: fetchMe, staleTime: 60_000, retry: false });
}

/** Convenience view over the current-user query for guards and the app chrome. */
export function useAuth() {
  const { data, isLoading } = useCurrentUser();
  return { user: data ?? null, isAuthenticated: !!data, isLoading };
}

/** Seed the cached identity after a successful sign-in so the UI updates without a round-trip. */
export function useSetCurrentUser() {
  const queryClient = useQueryClient();
  return (user: AuthUser) => queryClient.setQueryData(authKeys.me, user);
}

export function useLogout() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: authApi.logout,
    onSettled: () => queryClient.setQueryData(authKeys.me, null),
  });
}
