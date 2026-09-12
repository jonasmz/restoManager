# Docker y entorno de desarrollo

## Regla fundamental

**Nada de tecnología se instala en el host.** Ni el SDK de .NET, ni Node/npm, ni el
cliente de PostgreSQL. Cada tecnología corre y se desarrolla en **su propio
contenedor**. Todos los comandos (`dotnet`, `ng`, `npm`, `psql`, `dotnet ef`) se
ejecutan **dentro** del contenedor correspondiente con `docker compose run/exec`.

Lo único que se necesita en el host es **Docker** + **Docker Compose**.

## Contenedores (un servicio por tecnología)

`deploy/docker-compose.yml` (a crear en una tarea posterior) define:

| Servicio | Imagen base | Puerto host | Rol |
|---|---|---|---|
| `postgres` | `postgres:17` | 5432 | Motor de datos. Dos bases: `resto_identity` y `resto_business` |
| `backend-auth` | `mcr.microsoft.com/dotnet/sdk:10.0` | — (interno) | Auth API — `dotnet watch run` |
| `backend-business` | `mcr.microsoft.com/dotnet/sdk:10.0` | — (interno) | Business API — `dotnet watch run` |
| `frontend` | `node:22` | — (interno) | `ng serve --host 0.0.0.0 --poll 2000` |
| `nginx` | `nginx:1.27-alpine` | `GATEWAY_PORT` (80) | Reverse proxy: único punto de entrada publicado al host, rutea por path a los tres de arriba (`deploy/nginx/nginx.conf`) |

`frontend`, `backend-auth` y `backend-business` **no publican puerto al host** —
solo son alcanzables vía `nginx` o con `docker compose exec/run`. Evita exponer
directamente los API de desarrollo (sin TLS, CORS abierto por defecto en dev) y
resuelve de paso el acceso desde otros dispositivos de la LAN: como todo queda bajo
un único origin (el de `nginx`), el frontend no necesita saber la IP/dominio del
backend (ver `frontend/src/app/core/config/environment.ts`, config runtime vía
`public/env.js`) ni el backend necesita permitir CORS para el uso normal (`Cors:Origins`,
configurable por `.env`, queda solo para acceso directo a una API sin pasar por
`nginx`).

Notas de diseño:

- **Código montado como volumen** (`./backend/business:/src`, `./frontend:/app`) para
  hot-reload. Las carpetas pesadas se excluyen con volúmenes anónimos
  (`/src/**/bin`, `/src/**/obj`, `/app/node_modules`).
- **`backend-business`**: volumen con nombre `menu_images` montado en
  `/var/lib/resto/menu-images` (variable `Storage__MenuImagesPath`). Guarda las
  imágenes de la carta (Fase 11); la API las sirve como estáticos en `/media/menu`.
  Sin la variable, la API cae a `ContentRoot/media/menu` (útil fuera de Docker).
- Imágenes de **desarrollo basadas en el SDK**; los `Dockerfile` de producción
  (multi-stage, runtime-only) se harán aparte.
- Red única de compose; los servicios se referencian por nombre
  (`backend-auth`, `postgres`).
- **`postgres`**: script de init que crea las dos bases y monta
  `requirements/restaurant_schema.sql` para bootstrap de `resto_business` (o se
  aplica vía migraciones EF Core — decisión a confirmar; ver más abajo).
- **Healthchecks** en los tres servicios; `depends_on` con `condition:
  service_healthy` para que las APIs esperen a Postgres.
- **Secretos** (`POSTGRES_PASSWORD`, connection strings, clave de firma JWT de la
  Auth API) vía archivo **`.env`** (git-ignored) y `env_file` / `secrets` de compose.
  Nunca en el repo, nunca en `appsettings.json`.

## Comandos habituales (siempre contenedorizados)

```bash
# ---- Arranque ----
docker compose up -d                       # levanta postgres + ambas APIs + frontend
docker compose up -d --build               # reconstruye imágenes
docker compose logs -f backend-business
docker compose down                        # -v para borrar también los volúmenes de datos

# ---- Backend .NET ----
docker compose exec backend-business dotnet build
docker compose exec backend-business dotnet test
docker compose run --rm backend-business dotnet ef migrations add <Nombre> \
  --project src/RestoManager.Business.Infrastructure \
  --startup-project src/RestoManager.Business.Api
docker compose run --rm backend-business dotnet ef database update \
  --project src/RestoManager.Business.Infrastructure \
  --startup-project src/RestoManager.Business.Api
docker compose exec backend-auth dotnet test

# ---- Frontend Angular ----
docker compose exec frontend npm install <paquete>     # SOLO tras aprobación del usuario
docker compose exec frontend npm run lint
docker compose exec frontend npm test
docker compose exec frontend npx ng generate component features/inventory/inventory-list

# ---- PostgreSQL ----
docker compose exec postgres psql -U postgres -d resto_business
docker compose exec postgres psql -U postgres -d resto_identity
docker compose exec postgres pg_dump -U postgres resto_business > backup.sql
```

> Si necesitas ejecutar algo puntual sin dejar el contenedor corriendo, usa
> `docker compose run --rm <servicio> <comando>`.

## Puertos

| Acceso | URL |
|---|---|
| App completa (frontend + APIs, vía `nginx`) | http://localhost (o `http://<ip-del-host>` desde otro dispositivo de la LAN) |
| PostgreSQL | `localhost:5432` (usuario/clave del `.env`) |

`frontend`, `backend-auth` y `backend-business` no publican puerto propio (ver
tabla de servicios); todo pasa por `nginx` en el puerto `GATEWAY_PORT` (80 por
defecto). Para pegarle a una API puntual sin pasar por el proxy (Swagger,
debugging), usar `docker compose exec <servicio> curl http://localhost:8080/...`
o reexponer el puerto momentáneamente en `docker-compose.yml`.

Dentro de la red de compose, los servicios se refieren entre sí por nombre
(`http://backend-auth:8080`, etc.), no por `localhost`.

## Decisiones a confirmar antes de crear el compose

- ¿La base `resto_business` se crea aplicando `requirements/restaurant_schema.sql`
  directamente, o **solo** vía migraciones EF Core que deben reproducir ese esquema?
- ¿Se añade un servicio `postgres-test` separado para los tests de integración, o se
  usan Testcontainers (que a su vez necesitan el socket de Docker)?
- Versión exacta de la imagen de Node (`node:22` vs tag concreto).
- ¿`.env` único en `deploy/` o uno por servicio?
