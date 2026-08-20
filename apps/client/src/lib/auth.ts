import { betterAuth } from "better-auth";
import { createAuthMiddleware } from "better-auth/api";
import { bearer, jwt } from "better-auth/plugins";
import { loadEnvConfig } from "@next/env";
import path from "path";
import { Pool } from "pg";
import { generateFriendCode } from "./utils";

// load .env from root
loadEnvConfig(path.resolve(process.cwd(), "../../"));

export const auth = betterAuth({
  baseURL: process.env.BETTER_AUTH_URL ?? "http://localhost:3000",
  secret: process.env.BETTER_AUTH_SECRET,
  database: new Pool({
    connectionString: process.env.DATABASE_URL ?? "postgresql://ecjc:ecjc@localhost:5432/ecjc"
  }),
  account: {
    accountLinking: {
      enabled: true,
      trustedProviders: ["google"],
    },
  },
  user: {
    additionalFields: {
      firstName: { type: "string", required: false },
      lastName: { type: "string", required: false },
      friendCode: { type: "string", required: false },
      stripeCustomerId: { type: "string", required: false },
      isPenaltyEnabled: { type: "boolean", required: false },
      penaltyAmount: { type: "number", required: false },
    },
  },
  socialProviders: {
    google: {
      clientId: process.env.GOOGLE_CLIENT_ID!,
      clientSecret: process.env.GOOGLE_CLIENT_SECRET!,
      mapProfileToUser: (profile) => {
        return {
          firstName: profile.given_name,
          lastName: profile.family_name,
        };
      },
    },
  },
  plugins: [
    bearer(),
    jwt({
      jwks: {
        keyPairConfig: {
          // match same algorithm as used by better-auth for signing JWTs 
          alg: "ES256",
        },
      },
    }),
  ],
  hooks: {
    before: createAuthMiddleware(async (_ctx) => {
      if (!process.env.GOOGLE_CLIENT_ID && typeof window === "undefined") {
        console.warn("Missing GOOGLE_CLIENT_ID environment variable");
      }
    }),
  },
  databaseHooks: {
    user: {
      create: {
        before: async (user) => {
          return {
            data: {
              ...user,
              friendCode: generateFriendCode(),
              isPenaltyEnabled: false,
              penaltyAmount: 0,
            }
          };
        },
      },
    },
  },
});
