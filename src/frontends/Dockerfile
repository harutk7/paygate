FROM node:22-alpine AS build
RUN corepack enable && corepack prepare pnpm@10.32.1 --activate

ARG APP_NAME

WORKDIR /app

# Copy workspace config
COPY package.json pnpm-lock.yaml pnpm-workspace.yaml turbo.json ./

# Copy all package.json files for install
COPY packages/types/package.json packages/types/
COPY packages/api-client/package.json packages/api-client/
COPY packages/ui/package.json packages/ui/
COPY apps/landing/package.json apps/landing/
COPY apps/portal/package.json apps/portal/
COPY apps/backoffice/package.json apps/backoffice/

# Install dependencies
RUN pnpm install --frozen-lockfile

# Copy all source
COPY . .

# Build the specific app
RUN pnpm --filter "@payment-gateway/${APP_NAME}" build

# Nginx runtime
FROM nginx:alpine
ARG APP_NAME
COPY --from=build /app/apps/${APP_NAME}/dist /usr/share/nginx/html
# Nginx config will be mounted via ConfigMap
COPY nginx-${APP_NAME}.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
