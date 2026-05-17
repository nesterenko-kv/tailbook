FROM node:22-alpine AS build
RUN apk upgrade --no-cache
WORKDIR /app
ENV PNPM_HOME="/pnpm"
ENV PATH="$PNPM_HOME:$PATH"
RUN corepack enable
COPY package.json pnpm-lock.yaml pnpm-workspace.yaml turbo.json ./
COPY packages ./packages
COPY apps ./apps
RUN pnpm install --frozen-lockfile
RUN pnpm --filter client-web build

FROM node:22-alpine
RUN apk upgrade --no-cache
WORKDIR /app
ENV NODE_ENV=production
ENV PORT=3000
COPY --from=build /app/apps/client-web/.next/standalone ./
COPY --from=build /app/apps/client-web/.next/static ./apps/client-web/.next/static
EXPOSE 3000
CMD ["node", "apps/client-web/server.js"]
