import type { FieldValues, Path, UseFormSetError } from "react-hook-form";

import { ApiError } from "@/shared/api/api-error";

function toCamelCase(value: string): string {
  return value.length === 0 ? value : `${value[0]?.toLowerCase()}${value.slice(1)}`;
}

export function applyApiFieldErrors<TValues extends FieldValues>(
  error: unknown,
  setError: UseFormSetError<TValues>,
): boolean {
  if (!(error instanceof ApiError) || error.kind !== "validation") return false;

  let applied = false;

  for (const [backendField, messages] of Object.entries(error.fieldErrors)) {
    const message = messages[0];
    if (!message) continue;

    setError(toCamelCase(backendField) as Path<TValues>, {
      type: "server",
      message,
    });
    applied = true;
  }

  return applied;
}
