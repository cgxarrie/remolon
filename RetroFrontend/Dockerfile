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
ENV BACKEND_UPSTREAM=http://backend:8080
ENV NGINX_ENVSUBST_FILTER=^BACKEND_UPSTREAM$
EXPOSE 8080
