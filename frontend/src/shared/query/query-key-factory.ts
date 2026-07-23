import type { QueryKey } from "@tanstack/react-query";

export type QueryKeyFactory<TScope extends string> = {
  all: readonly [TScope];
  lists: () => readonly [TScope, "list"];
  list: <TFilters extends object>(filters: TFilters) => readonly [TScope, "list", TFilters];
  details: () => readonly [TScope, "detail"];
  detail: (id: string) => readonly [TScope, "detail", string];
};

export function createQueryKeyFactory<TScope extends string>(
  scope: TScope,
): QueryKeyFactory<TScope> {
  return {
    all: [scope] as const,
    lists: () => [scope, "list"] as const,
    list: <TFilters extends object>(filters: TFilters) =>
      [scope, "list", filters] as const,
    details: () => [scope, "detail"] as const,
    detail: (id: string) => [scope, "detail", id] as const,
  };
}

export function isQueryKey(value: readonly unknown[]): value is QueryKey {
  return value.length > 0;
}
