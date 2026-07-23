import type { InputHTMLAttributes, ReactNode } from "react";
import type { FieldError } from "react-hook-form";

import "@/shared/form/form.css";

interface FormFieldProps extends InputHTMLAttributes<HTMLInputElement> {
  label: string;
  error?: FieldError;
  hint?: ReactNode;
}

export function FormField({ label, error, hint, id, ...inputProps }: FormFieldProps) {
  const inputId = id ?? inputProps.name;
  const errorId = error && inputId ? `${inputId}-error` : undefined;
  const hintId = hint && inputId ? `${inputId}-hint` : undefined;
  const describedBy = [hintId, errorId].filter(Boolean).join(" ") || undefined;

  return (
    <div className="form-field">
      <label className="form-field__label" htmlFor={inputId}>{label}</label>
      <input
        {...inputProps}
        id={inputId}
        className="form-field__control"
        aria-invalid={error ? true : undefined}
        aria-describedby={describedBy}
      />
      {hint ? <div id={hintId} className="form-field__hint">{hint}</div> : null}
      {error ? <div id={errorId} className="form-field__error" role="alert">{error.message}</div> : null}
    </div>
  );
}

export function FormError({ message }: { message?: string }) {
  if (!message) return null;
  return <div className="form-error" role="alert">{message}</div>;
}
