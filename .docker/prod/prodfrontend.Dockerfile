FROM node:20 AS builder

WORKDIR /app

COPY ./frontend/package*.json ./

RUN npm ci 

COPY ./frontend .

RUN npm run build

FROM nginx:alpine

RUN apk add --no-cache curl

COPY --from=builder /app/dist /usr/share/nginx/html

COPY ./.docker/prod/nginx/nginx.conf /etc/nginx/nginx.conf

RUN chown -R nginx:nginx /var/cache/nginx && \
    chown -R nginx:nginx /var/log/nginx && \
    chown -R nginx:nginx /etc/nginx/conf.d

RUN mkdir -p /var/run && \
    chown -R nginx:nginx /var/run

EXPOSE 8080

CMD ["nginx", "-g", "daemon off;"]