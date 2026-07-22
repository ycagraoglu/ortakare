import { z } from "zod";

const apiUrlSchema = z
  .string()
  .url()
  .transform((value) => value.replace(/\/$/, ""))
  .superRefine((value, context) => {
    if (!import.meta.env.PROD) return;

    const url = new URL(value);
    if (url.protocol !== "https:") {
      context.addIssue({
        code: "custom",
        message: "Production API URL must use HTTPS.",
      });
    }
  });

const envSchema = z.object({
  VITE_API_URL: apiUrlSchema,
});

const result = envSchema.safeParse(import.meta.env);

if (!result.success) {
  console.error("Frontend environment configuration is invalid.", result.error.flatten().fieldErrors);
  throw new Error("Frontend environment configuration is invalid.");
}

export const env = result.data;
