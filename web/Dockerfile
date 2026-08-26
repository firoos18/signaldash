# SignalDash Web - NextJS 16
FROM node:22-alpine AS build
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm ci
COPY . .
# API URL baked at build: cluster service DNS
ARG NEXT_PUBLIC_API_URL=http://signaldash-api.signaldash.svc.cluster.local:8080
ENV NEXT_PUBLIC_API_URL=$NEXT_PUBLIC_API_URL
# ponytail: webpack flag — Turbopack crashes on constrained VMs (worker error);
#           webpack build is slower but reliable. Revisit on Next 17.
RUN npx next build --webpack

FROM node:22-alpine
WORKDIR /app
ENV NODE_ENV=production
COPY --from=build /app/.next ./.next
COPY --from=build /app/node_modules ./node_modules
COPY --from=build /app/package.json ./package.json
COPY --from=build /app/public ./public
EXPOSE 3000
CMD ["npx", "next", "start"]
