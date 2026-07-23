import { describe, expect, it } from "vitest";

import {
  clearStoredSession,
  getRememberedEmail,
  getStoredRefreshToken,
  getStoredUser,
  saveRememberedEmail,
  saveStoredSession,
  updateStoredRefreshToken,
} from "@/features/auth/model/auth-storage";

const user = {
  id: "user-1",
  displayName: "Yusuf Çağraoğlu",
  email: "yusuf@example.com",
};

describe("auth storage", () => {
  it("stores refresh token and user only in sessionStorage", () => {
    saveStoredSession({ refreshToken: "refresh-token", user });

    expect(getStoredRefreshToken()).toBe("refresh-token");
    expect(getStoredUser()).toEqual(user);
    expect(window.localStorage.getItem("ortakare.auth.refresh-token")).toBeNull();
    expect(window.localStorage.getItem("ortakare.auth.user")).toBeNull();
  });

  it("updates the refresh token without persisting it", () => {
    saveStoredSession({ refreshToken: "old-token", user });
    updateStoredRefreshToken("new-token");

    expect(getStoredRefreshToken()).toBe("new-token");
    expect(window.localStorage.getItem("ortakare.auth.refresh-token")).toBeNull();
  });

  it("stores only the normalized remembered email in localStorage", () => {
    saveRememberedEmail("  Yusuf@Example.COM ");

    expect(getRememberedEmail()).toBe("yusuf@example.com");
    expect(getStoredRefreshToken()).toBeNull();
  });

  it("clears current and legacy auth values", () => {
    saveStoredSession({ refreshToken: "refresh-token", user });
    window.localStorage.setItem("ortakare.auth.refresh-token", "legacy-token");
    window.localStorage.setItem("ortakare.auth.user", JSON.stringify(user));

    clearStoredSession();

    expect(getStoredRefreshToken()).toBeNull();
    expect(getStoredUser()).toBeNull();
    expect(window.localStorage.getItem("ortakare.auth.refresh-token")).toBeNull();
    expect(window.localStorage.getItem("ortakare.auth.user")).toBeNull();
  });
});
