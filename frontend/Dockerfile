# ------------------------------------------------------------
# frontend — Placeholder Dockerfile (T-104 Day-1 skeleton)
#
# TODO (T-105): Replace this placeholder with the real multi-
# stage build once the React + Vite + TS project exists:
#
#   Stage 1 (build):
#     FROM node:24-alpine AS build
#     WORKDIR /app
#     COPY package*.json .
#     RUN npm ci
#     COPY . .
#     RUN npm run build
#
#   Stage 2 (serve):
#     FROM nginx:1.27-alpine
#     COPY --from=build /app/dist /usr/share/nginx/html
#     COPY nginx.conf /etc/nginx/conf.d/default.conf
#     EXPOSE 80
# ------------------------------------------------------------
FROM nginx:1.27-alpine

# Placeholder: serve a minimal status page so the container
# starts cleanly and docker compose config validates.
RUN echo '<html><body><h1>frontend placeholder — replace with real build (T-105)</h1></body></html>' \
    > /usr/share/nginx/html/index.html

EXPOSE 80
