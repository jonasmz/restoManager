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
| `backend-auth` | `mcr.microsoft.com/dotnet/sdk:10.0` | 5001 | Auth API — `dotnet watch run` |
| `backend-business` | `mcr.microsoft.com/dotnet/sdk:10.0` | 5002 | Business API — `dotnet watch run` |
| `frontend` | `node:22` | 4200 | `ng serve --host 0.0.0.0 --poll 2000` |

Notas de diseño:

- **Código montado como volumen** (`./backend/business:/src`, `./frontend:/app`) para
  hot-reload. Las carpetas pesadas se excluyen con volúmenes anónimos
  (`/src/**/bin`, `/src/**/obj`, `/app/node_modules`).
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

| Servicio | URL de desarrollo |
|---|---|
| Frontend | http://localhost:4200 |
| Auth API | http://localhost:5001 (Swagger en `/swagger`) |
| Business API | http://localhost:5002 (Swagger en `/swagger`) |
| PostgreSQL | `localhost:5432` (usuario/clave del `.env`) |

Dentro de la red de compose, el frontend y la Business API se refieren a las otras
por nombre de servicio (`http://backend-auth:5001`, etc.), no por `localhost`.

## Decisiones a confirmar antes de crear el compose

- ¿La base `resto_business` se crea aplicando `requirements/restaurant_schema.sql`
  directamente, o **solo** vía migraciones EF Core que deben reproducir ese esquema?
- ¿Se añade un servicio `postgres-test` separado para los tests de integración, o se
  usan Testcontainers (que a su vez necesitan el socket de Docker)?
- Versión exacta de la imagen de Node (`node:22` vs tag concreto).
- ¿`.env` único en `deploy/` o uno por servicio?
