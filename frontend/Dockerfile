# Stage 1: Build
FROM node:22-alpine AS build
WORKDIR /app

# Copy package files
COPY package*.json ./

# Install dependencies
RUN npm ci

# Copy source code
COPY . .

# Receive build-time env from docker-compose build.args.
# VITE_* variables are inlined statically by Vite at build time;
# passing them as runtime environment variables has no effect.
ARG VITE_API_BASE_URL=http://localhost:5000
ENV VITE_API_BASE_URL=$VITE_API_BASE_URL

# Build the Vite app (VITE_API_BASE_URL is now available to Vite)
RUN npm run build

# Stage 2: Serve
FROM nginx:1.27-alpine

# Copy the build output
COPY --from=build /app/dist /usr/share/nginx/html

# Copy nginx configuration for SPA routing
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
