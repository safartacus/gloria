import { QueryClient } from '@tanstack/react-query';
import { ApiError } from '../api/client.ts';

export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      // don't retry client errors (404/409/…) — only transient ones like a 5xx or a dropped connection
      retry: (failureCount, error) => {
        if (error instanceof ApiError && error.status < 500) return false;
        return failureCount < 1;
      },
      refetchOnWindowFocus: false,
      staleTime: 30_000,
    },
  },
});
