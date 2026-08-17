import { betterAuth } from "better-auth";
import { createAuthMiddleware } from "better-auth/api";
import { bearer, jwt } from "better-auth/plugins";
import { ApiClient } from "./api";
import { loadEnvConfig } from "@next/env";
import path from "path";

// load .env from root
loadEnvConfig(path.resolve(process.cwd(), "../../"));

const backendUrl =
  process.env.NODE_ENV === "production"
    ? (process.env.API_URL ?? "")
    : "http://localhost:5186";

export const auth = betterAuth({
  baseURL: process.env.BETTER_AUTH_URL ?? "http://localhost:3000",
  secret: process.env.BETTER_AUTH_SECRET,
  socialProviders: {
    google: {
      clientId: process.env.GOOGLE_CLIENT_ID!,
      clientSecret: process.env.GOOGLE_CLIENT_SECRET!,
    },
  },
  plugins: [
    bearer(),
    jwt(),
  ],
  hooks: {
    before: createAuthMiddleware(async (_ctx) => {
      if (!process.env.GOOGLE_CLIENT_ID) {
        throw new Error("Missing GOOGLE_CLIENT_ID environment variable");
      }
      if (!process.env.GOOGLE_CLIENT_SECRET) {
        throw new Error("Missing GOOGLE_CLIENT_SECRET environment variable");
      }
    }),
  },
  databaseHooks: {
    user: {
      create: {
        after: async (user) => {
          const client = new ApiClient(backendUrl, { fetch: (...args) => fetch(...args) });
          try {
            await client.postApiUsersSync({
              id: user.id,
              email: user.email,
              name: user.name ?? undefined,
              image: user.image ?? undefined,
            });
          } catch (error: any) {
            console.error(`Failed to sync user to backend: ${error.message}`);
          }
        },
      },
      update: {
        after: async (user) => {
          const client = new ApiClient(backendUrl, { fetch: (...args) => fetch(...args) });
          try {
            await client.postApiUsersSync({
              id: user.id,
              email: user.email,
              name: user.name ?? undefined,
              image: user.image ?? undefined,
            });
          } catch (error: any) {
            console.error(`Failed to sync user to backend: ${error.message}`);
          }
        },
      },
    },
  },
});
