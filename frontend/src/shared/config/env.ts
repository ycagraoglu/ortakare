import { z } from "zod";

const secureUrlSchema = z
  .string()
  .url()
  .transform((value) => value.replace(/\/$/, ""))
  .superRefine((value, context) => {
    if (!import.meta.env.PROD) return;

    const url = new URL(value);
    if (url.protocol !== "https:") {
      context.addIssue({
        code: "custom",
        message: "Production URLs must use HTTPS.",
      });
    }
  });

const envSchema = z.object({
  VITE_API_URL: secureUrlSchema,
  VITE_TELEMETRY_URL: z.union([secureUrlSchema, z.literal("")]).optional(),
  VITE_RELEASE: z.string().trim().max(100).optional(),
});

const result = envSchema.safeParse(import.meta.env);

if (!result.success) {
  console.error("Frontend environment configuration is invalid.", result.error.flatten().fieldErrors);
  throw new Error("Frontend environment configuration is invalid.");
}

export const env = result.data;
