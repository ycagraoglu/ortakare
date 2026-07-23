import { describe, expect, it } from "vitest";

import { ApiError } from "@/shared/api/api-error";
import {
  unwrapApiResult,
  unwrapApiResultWithoutData,
} from "@/shared/api/unwrap-api-result";

describe("unwrapApiResult", () => {
  it("returns the data for a successful backend response", () => {
    const result = unwrapApiResult<{ id: string }>({
      isSuccess: true,
      statusCode: 200,
      message: null,
      data: { id: "event-1" },
    });

    expect(result).toEqual({ id: "event-1" });
  });

  it("throws ApiError for a failed backend response", () => {
    expect(() =>
      unwrapApiResult({
        isSuccess: false,
        statusCode: 409,
        message: "Aktif dışa aktarım zaten mevcut.",
        data: null,
      }),
    ).toThrowError(ApiError);
  });

  it("rejects malformed successful responses", () => {
    expect(() => unwrapApiResult({ ok: true })).toThrowError(
      "Sunucudan beklenmeyen bir yanıt alındı.",
    );
  });
});

describe("unwrapApiResultWithoutData", () => {
  it("accepts a successful response without data", () => {
    expect(() =>
      unwrapApiResultWithoutData({
        isSuccess: true,
        statusCode: 204,
        message: null,
      }),
    ).not.toThrow();
  });
});
