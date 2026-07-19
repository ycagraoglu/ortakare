import { MutationCache, QueryCache, QueryClient } from "@tanstack/react-query";

import { queryRetryDelay, shouldRetryQuery } from "@/shared/query/query-policy";

const DEFAULT_STALE_TIME_MS = 30_000;
const DEFAULT_GC_TIME_MS = 5 * 60_000;

export const queryClient = new QueryClient({
  queryCache: new QueryCache(),
  mutationCache: new MutationCache(),
  defaultOptions: {
    queries: {
      staleTime: DEFAULT_STALE_TIME_MS,
      gcTime: DEFAULT_GC_TIME_MS,
      retry: shouldRetryQuery,
      retryDelay: queryRetryDelay,
      refetchOnWindowFocus: false,
      refetchOnReconnect: true,
    },
    mutations: {
      retry: false,
    },
  },
});

export async function clearServerState(): Promise<void> {
  await queryClient.cancelQueries();
  queryClient.clear();
}
