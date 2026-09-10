# Stage 1: build the React app
FROM node:22-alpine AS build
WORKDIR /app
COPY package*.json ./
RUN npm ci
COPY . .
RUN npm run build

# Stage 2: serve via nginx (non-root, port 8080)
FROM nginxinc/nginx-unprivileged:alpine
COPY --from=build /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/templates/default.conf.template
# Compose defaults. On Railway set BACKEND_UPSTREAM to the private backend
# (http://<service>.railway.internal:8080) and NGINX_RESOLVER to [fd12::10] ipv6=on valid=1s
ENV BACKEND_UPSTREAM=http://backend:8080
ENV NGINX_RESOLVER="127.0.0.11 ipv6=off valid=10s"
ENV NGINX_ENVSUBST_FILTER=^(BACKEND_UPSTREAM|NGINX_RESOLVER)$
EXPOSE 8080
