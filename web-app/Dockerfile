FROM node:20-alpine AS build

WORKDIR /usr/local/app

COPY package*.json ./

RUN npm ci

COPY . .

RUN npm run build -- --configuration=production

FROM nginx:1.25-alpine

RUN rm -rf /usr/share/nginx/html/*

COPY --from=build /usr/local/app/dist/web-app /usr/share/nginx/html

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
