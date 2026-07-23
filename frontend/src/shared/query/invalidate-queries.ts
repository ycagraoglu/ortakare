import type { QueryKey } from "@tanstack/react-query";

import { queryClient } from "@/shared/query/query-client";

export async function invalidateQueries(...queryKeys: QueryKey[]): Promise<void> {
  await Promise.all(
    queryKeys.map((queryKey) =>
      queryClient.invalidateQueries({
        queryKey,
        exact: false,
      }),
    ),
  );
}

export async function removeQueries(queryKey: QueryKey): Promise<void> {
  await queryClient.cancelQueries({ queryKey });
  queryClient.removeQueries({ queryKey });
}
