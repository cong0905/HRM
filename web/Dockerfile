# Stage 1: Build Web Bundle
FROM node:24-alpine AS build
WORKDIR /app

# Copy contracts workspace needed by hrm-api-contracts
COPY contracts /contracts
WORKDIR /contracts
RUN npm ci --ignore-scripts --no-audit --no-fund

# Copy web package manifests and install dependencies
WORKDIR /app
COPY web/package*.json ./
RUN npm ci --ignore-scripts --no-audit --no-fund

# Copy web source files and build
COPY web/ ./
RUN npm run build

# Stage 2: Production Nginx Static Server
FROM nginx:alpine AS final

# Remove default nginx site
RUN rm -rf /etc/nginx/conf.d/default.conf

# Copy custom Nginx configuration
COPY web/nginx.conf /etc/nginx/conf.d/default.conf

# Copy compiled static assets from build stage
COPY --from=build /app/dist /usr/share/nginx/html

EXPOSE 80

HEALTHCHECK --interval=10s --timeout=3s --retries=3 \
  CMD wget --quiet --tries=1 --spider http://localhost/healthz || exit 1

CMD ["nginx", "-g", "daemon off;"]
