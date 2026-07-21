import { z } from "zod";

export const loginSchema = z.object({
  email: z.email({ message: "Geçerli bir e-posta adresi girin." }),
  password: z.string().min(1, "Şifre zorunludur."),
  rememberMe: z.boolean(),
});

export type LoginFormValues = z.infer<typeof loginSchema>;
