import { useQuery } from '@tanstack/react-query';

import { authApi } from '@/lib/api/auth';

/**
 * Anonymous config for the sign-in / register screens: whether self-service registration is open, and an
 * optional sign-in hint (e.g. shared demo credentials). Cached briefly — it changes rarely.
 */
export function usePublicConfig() {
  return useQuery({
    queryKey: ['auth', 'public-config'],
    queryFn: authApi.publicConfig,
    staleTime: 5 * 60_000,
  });
}
