import { zodResolver } from "@hookform/resolvers/zod";
import { useState } from "react";
import { useForm } from "react-hook-form";
import { useLocation, useNavigate } from "react-router-dom";

import { useAuth } from "@/features/auth/hooks/use-auth";
import { getRememberedEmail } from "@/features/auth/model/auth-storage";
import {
  loginSchema,
  type LoginFormValues,
} from "@/features/auth/model/login-schema";
import { ApiError } from "@/shared/api/api-error";
import {
  applyApiFieldErrors,
  FormError,
  FormField,
} from "@/shared/form";

interface LoginLocationState {
  from?: string;
}

export default function LoginPage() {
  const { login } = useAuth();
  const location = useLocation();
  const navigate = useNavigate();
  const [submitError, setSubmitError] = useState<string>();
  const rememberedEmail = getRememberedEmail();

  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<LoginFormValues>({
    resolver: zodResolver(loginSchema),
    defaultValues: {
      email: rememberedEmail,
      password: "",
      rememberMe: rememberedEmail.length > 0,
    },
    mode: "onBlur",
  });

  const onSubmit = handleSubmit(async (values) => {
    setSubmitError(undefined);

    try {
      await login(
        {
          email: values.email,
          password: values.password,
        },
        values.rememberMe,
      );

      const state = location.state as LoginLocationState | null;
      navigate(state?.from ?? "/dashboard", { replace: true });
    } catch (error) {
      if (applyApiFieldErrors(error, setError)) return;
      setSubmitError(error instanceof ApiError ? error.message : "Giriş sırasında beklenmeyen bir hata oluştu.");
    }
  });

  return (
    <section aria-labelledby="login-title">
      <h1 id="login-title">Giriş</h1>
      <p>Ortakare hesabınızla yönetim paneline giriş yapın.</p>

      <form className="form-shell" noValidate onSubmit={onSubmit}>
        <FormError message={submitError} />

        <FormField
          label="E-posta"
          type="email"
          autoComplete="email"
          error={errors.email}
          {...register("email")}
        />

        <FormField
          label="Şifre"
          type="password"
          autoComplete="current-password"
          error={errors.password}
          {...register("password")}
        />

        <label>
          <input type="checkbox" {...register("rememberMe")} />{" "}
          E-posta adresimi bu cihazda hatırla
        </label>

        <div className="form-actions">
          <button className="form-submit" type="submit" disabled={isSubmitting}>
            {isSubmitting ? "Giriş yapılıyor…" : "Giriş yap"}
          </button>
        </div>
      </form>
    </section>
  );
}
