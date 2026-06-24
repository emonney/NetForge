import { QueryClient } from '@tanstack/react-query';

import { isApiError } from '@/lib/problem';

export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      refetchOnWindowFocus: false,
      staleTime: 30_000,
      retry: (failureCount, error) => {
        // 4xx (auth, validation, not-found) won't fix themselves — only retry transient 5xx.
        if (isApiError(error) && error.status < 500) return false;
        return failureCount < 2;
      },
    },
  },
});
