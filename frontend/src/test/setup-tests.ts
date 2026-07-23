import "@testing-library/jest-dom/vitest";
import { cleanup } from "@testing-library/react";
import { afterAll, afterEach, beforeAll } from "vitest";

import { testServer } from "@/test/server";

beforeAll(() => {
  testServer.listen({ onUnhandledRequest: "error" });
});

afterEach(() => {
  cleanup();
  testServer.resetHandlers();
  window.localStorage.clear();
  window.sessionStorage.clear();
});

afterAll(() => {
  testServer.close();
});
