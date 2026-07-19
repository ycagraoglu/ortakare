import { describe, expect, it } from "vitest";

import { ApiError } from "@/shared/api";
import { queryRetryDelay, shouldRetryQuery } from "@/shared/query/query-policy";

describe("shouldRetryQuery", () => {
  it.each(["network", "timeout", "server"] as const)(
    "%s hatalarını sınırlı sayıda tekrar dener",
    (kind) => {
      const error = new ApiError({ kind, message: "Geçici hata" });

      expect(shouldRetryQuery(0, error)).toBe(true);
      expect(shouldRetryQuery(1, error)).toBe(true);
      expect(shouldRetryQuery(2, error)).toBe(false);
    },
  );

  it.each([
    "unauthorized",
    "forbidden",
    "not-found",
    "conflict",
    "validation",
    "rate-limit",
    "aborted",
  ] as const)("%s hatalarını tekrar denemez", (kind) => {
    const error = new ApiError({ kind, message: "Kalıcı hata" });

    expect(shouldRetryQuery(0, error)).toBe(false);
  });

  it("tanımsız hata türlerini tekrar denemez", () => {
    expect(shouldRetryQuery(0, new Error("Bilinmeyen"))).toBe(false);
  });
});

describe("queryRetryDelay", () => {
  it("üstel gecikmeyi üst sınırla uygular", () => {
    expect(queryRetryDelay(0)).toBe(1_000);
    expect(queryRetryDelay(1)).toBe(2_000);
    expect(queryRetryDelay(4)).toBe(8_000);
  });
});
