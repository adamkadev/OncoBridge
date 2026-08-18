FROM node:24-alpine AS build

WORKDIR /app

COPY src/oncobridge-web/package.json src/oncobridge-web/package-lock.json ./

RUN npm ci

COPY src/oncobridge-web/ ./

RUN npm run build

FROM nginx:1.29-alpine AS runtime

COPY docker/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist/oncobridge-web/browser /usr/share/nginx/html

EXPOSE 80
