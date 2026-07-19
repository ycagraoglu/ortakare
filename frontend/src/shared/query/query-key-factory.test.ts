import { describe, expect, it } from "vitest";

import { createQueryKeyFactory } from "@/shared/query/query-key-factory";

describe("createQueryKeyFactory", () => {
  const eventKeys = createQueryKeyFactory("events");

  it("scope altındaki liste anahtarlarını üretir", () => {
    expect(eventKeys.all).toEqual(["events"]);
    expect(eventKeys.lists()).toEqual(["events", "list"]);
    expect(eventKeys.list({ page: 1, search: "düğün" })).toEqual([
      "events",
      "list",
      { page: 1, search: "düğün" },
    ]);
  });

  it("detay anahtarlarını aynı hiyerarşide üretir", () => {
    expect(eventKeys.details()).toEqual(["events", "detail"]);
    expect(eventKeys.detail("event-id")).toEqual([
      "events",
      "detail",
      "event-id",
    ]);
  });
});
