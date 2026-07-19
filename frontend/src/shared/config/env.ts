import { z } from "zod";

const envSchema = z.object({
  VITE_API_URL: z.string().url().transform((value) => value.replace(/\/$/, "")),
});

const result = envSchema.safeParse(import.meta.env);

if (!result.success) {
  console.error("Frontend environment configuration is invalid.", result.error.flatten().fieldErrors);
  throw new Error("Frontend environment configuration is invalid.");
}

export const env = result.data;
