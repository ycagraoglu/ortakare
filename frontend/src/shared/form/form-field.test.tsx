import { screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";

import { FormError, FormField } from "@/shared/form/form-field";
import { renderApp } from "@/test/render";

describe("FormField", () => {
  it("associates label, hint and validation error with the input", () => {
    renderApp(
      <FormField
        name="email"
        label="E-posta"
        hint="Kurumsal e-posta adresinizi girin."
        error={{ type: "validate", message: "E-posta geçersiz." }}
      />,
    );

    const input = screen.getByRole("textbox", { name: "E-posta" });

    expect(input).toHaveAttribute("aria-invalid", "true");
    expect(input).toHaveAttribute("aria-describedby", "email-hint email-error");
    expect(screen.getByText("E-posta geçersiz.")).toHaveAttribute("role", "alert");
  });

  it("announces form-level errors", () => {
    renderApp(<FormError message="İşlem tamamlanamadı." />);

    expect(screen.getByRole("alert")).toHaveTextContent("İşlem tamamlanamadı.");
  });
});
