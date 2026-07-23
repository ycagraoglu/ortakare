import { describe, expect, it } from "vitest";

import {
  sanitizeApiPath,
  sanitizeErrorMessage,
  sanitizeRoute,
} from "@/shared/observability/sanitize-telemetry";

describe("telemetry sanitization", () => {
  it("removes query strings, hashes and route identifiers", () => {
    expect(
      sanitizeRoute(
        "/events/019b6c57-6ab8-7b04-a594-bc158f813e71/photos?token=secret#preview",
      ),
    ).toBe("/events/:id/photos");

    expect(sanitizeRoute("/participants/1452")).toBe("/participants/:id");
  });

  it("redacts email and identifier values from error messages", () => {
    const message = sanitizeErrorMessage(
      "yusuf@example.com kullanıcısı için 019b6c57-6ab8-7b04-a594-bc158f813e71 kaydı bulunamadı",
    );

    expect(message).toBe("[email] kullanıcısı için [id] kaydı bulunamadı");
  });

  it("normalizes absolute API URLs without retaining query values", () => {
    expect(
      sanitizeApiPath(
        "https://api.example.com/api/events/019b6c57-6ab8-7b04-a594-bc158f813e71?include=photos",
      ),
    ).toBe("/api/events/:id");
  });
});
