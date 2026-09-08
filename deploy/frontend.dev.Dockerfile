# Imagen de desarrollo para el frontend Angular. El código se monta como volumen;
# `node_modules` vive en un volumen anónimo (ver docker-compose).
FROM node:22-alpine

# npm 11: evita el fallo de arborist ("edgesOut") de npm 10 con volúmenes montados.
RUN npm install -g npm@11 \
 && apk add --no-cache curl

ENV NG_CLI_ANALYTICS=false \
    CI=1

WORKDIR /app
EXPOSE 4200

# `npm install` en el arranque (node_modules es un volumen vacío la primera vez).
CMD ["sh", "-lc", "npm install --no-fund --no-audit && npx ng serve --host 0.0.0.0 --port 4200 --poll 2000"]
