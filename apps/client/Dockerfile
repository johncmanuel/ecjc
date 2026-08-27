FROM node:24-alpine AS base

FROM base AS deps
WORKDIR /app
# Check https://github.com/nodejs/docker-node/tree/b4117f9333da4138b03a546ec926ef50a31506c3#nodealpine to understand why libc6-compat might be needed.
RUN apk add --no-cache libc6-compat
COPY package.json package-lock.json ./
COPY apps/client/package.json ./apps/client/package.json
RUN npm ci
RUN mkdir -p /app/apps/client/node_modules

FROM base AS builder
WORKDIR /app
# considering not to use npm workspaces in the future since i have to deal with dependencies quirks, esp when all dependnecies are
# located at root and have to manually move this 
COPY --from=deps /app/node_modules ./node_modules
COPY --from=deps /app/apps/client/node_modules ./apps/client/node_modules
COPY . .
ARG API_URL
ENV API_URL=$API_URL
RUN npm run build -w client

FROM base AS runner
WORKDIR /app
ENV NODE_ENV=production
ENV NEXT_TELEMETRY_DISABLED=1

RUN addgroup --system --gid 1001 nodejs
RUN adduser --system --uid 1001 nextjs

COPY --from=builder /app/node_modules ./node_modules
COPY --from=builder /app/package.json ./package.json
COPY --from=builder /app/apps/client/public ./apps/client/public
COPY --from=builder --chown=nextjs:nodejs /app/apps/client/.next ./apps/client/.next
COPY --from=builder /app/apps/client/node_modules ./apps/client/node_modules
COPY --from=builder /app/apps/client/package.json ./apps/client/package.json
COPY --from=builder /app/apps/client/next.config.* ./apps/client/

USER nextjs

EXPOSE 3000
ENV PORT=3000
ENV HOSTNAME="0.0.0.0"

WORKDIR /app/apps/client
CMD ["npm", "start"]
