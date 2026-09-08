# Fase 0 — Andamiaje

## Objetivo y alcance

Dejar el monorepo listo para desarrollar, **sin nada de dominio**:

- `docker compose up` levanta 4 servicios: `postgres`, `backend-auth`,
  `backend-business`, `frontend`.
- Ambas APIs .NET 10 compilan, exponen `/health` y `/swagger`, con la estructura
  hexagonal de 4 proyectos (Domain / Application / Infrastructure / Api) vacía.
- El frontend Angular 22 muestra el **shell del template** (`requirements/inapp/`):
  topbar fijo, sidebar colapsable, overlay móvil, y rutas placeholder navegables.

**Fuera de alcance:** entidades, casos de uso, endpoints reales, migraciones, auth
funcional (solo el plumbing).

## Dependencias

Ninguna. Decisiones bloqueantes: ninguna.

## Infraestructura (`deploy/`)

- `deploy/docker-compose.yml` con los 4 servicios (ver [`../docker.md`](../docker.md)):
  - `postgres`: `postgres:17`, volumen `pgdata`, monta `deploy/postgres/init.sql`,
    puerto 5432, healthcheck `pg_isready`.
  - `backend-auth`, `backend-business`: build desde `deploy/backend.dev.Dockerfile`
    (base `mcr.microsoft.com/dotnet/sdk:10.0`), código montado, `dotnet watch run`,
    puertos 5001 / 5002, `depends_on: postgres (service_healthy)`.
  - `frontend`: build desde `deploy/frontend.dev.Dockerfile` (base `node:22`), código
    montado, `node_modules` en volumen anónimo, `ng serve --host 0.0.0.0 --poll 2000`,
    puerto 4200.
  - Red común `resto`; `env_file: ../.env`.
- `deploy/postgres/init.sql`: crea `resto_identity` y `resto_business`.
- `deploy/.env.example`: plantilla sin secretos (`POSTGRES_*`,
  `ConnectionStrings__*`, `AUTH__ISSUER`, `AUTH__SIGNING_KEY_PATH`). El `.env` real
  es local y está en `.gitignore`.
- `deploy/backend.dev.Dockerfile`, `deploy/frontend.dev.Dockerfile`.

## Backend (`backend/auth/`, `backend/business/`)

Para cada solución (`RestoManager.<Servicio>.<Capa>`, ver
[`../arquitectura.md`](../arquitectura.md)):

- `dotnet new sln -n RestoManager.<Servicio>`.
- Proyectos: `Domain` (classlib), `Application` (classlib), `Infrastructure`
  (classlib), `Api` (`webapi`, minimal API). Referencias hacia adentro:
  `Api → Application → Domain`, `Infrastructure → Application/Domain`,
  `Api → Infrastructure` (solo para composición en `Program.cs`).
- `Directory.Build.props` en la raíz de cada solución: `Nullable=enable`,
  `ImplicitUsings=enable`, `TreatWarningsAsErrors=true`, `LangVersion=latest`,
  `AnalysisLevel=latest-recommended`. `.editorconfig` compartido.
- `Api/Program.cs`: `AddHealthChecks` + `MapHealthChecks("/health")`, OpenAPI
  (`AddOpenApi`) + Swagger UI en Development, `AddProblemDetails`, CORS para
  `http://localhost:4200`, `IOptions` para configuración.
- `Infrastructure`: paquetes `Microsoft.EntityFrameworkCore` +
  `Npgsql.EntityFrameworkCore.PostgreSQL`; `DbContext` **vacío**
  (`AuthDbContext` / `BusinessDbContext`) con su connection string, registrado en DI.
  **Sin entidades ni migraciones.**
- `backend/business` además: `Microsoft.AspNetCore.Authentication.JwtBearer`,
  `AddAuthentication().AddJwtBearer(...)` leyendo `Auth:Authority` de configuración.
  Todavía **sin** `[Authorize]` global.
- `tests/RestoManager.<Servicio>.Tests` (xUnit) con un test trivial que compila y pasa.

## Frontend (`frontend/`)

- Generado en el contenedor: `ng new frontend --style=scss --routing --ssr=false
  --skip-git --package-manager=npm`.
- Dependencias (instaladas en el contenedor): `bootstrap`, `@popperjs/core`,
  `@tabler/icons-webfont`, `apexcharts`, `ng-apexcharts`.
- Estructura `src/app/{core,shared,layout,features}` (ver
  [`../frontend.md`](../frontend.md)).
- `src/styles/`: copia de `requirements/inapp/src/assets/scss/*` (`_variables.scss`,
  `_custom.scss`, `_avatar.scss`, `_border.scss`, `_button.scss`, `_icon-shape.scss`,
  `_utilities.scss`) + `styles.scss` que importa en el orden del template:
  fuentes Poppins → Tabler webfont → `variables` → `bootstrap/scss/bootstrap` →
  parciales → `custom`. Registrado en `angular.json`.
- `layout/layout.component`: shell del template — topbar 60px (botón toggle desktop
  `#toggleBtn`, botón móvil `#mobileBtn`, campana con badge, dropdown de usuario),
  sidebar 240/60px con grupos "Main" (Dashboard, Inventario, Reportes) y "Account"
  (Iniciar sesión, Registro), `routerLinkActive`, overlay móvil. Estado del sidebar
  en `layout/layout.service` (señal `collapsed`/`mobileOpen`), **sin** tocar el DOM.
- `core/http/`: `authInterceptor` y `errorInterceptor` funcionales, **registrados
  pero sin lógica** (stubs con TODO).
- `core/config/environment*`: `authApiUrl` (`http://localhost:5001`),
  `businessApiUrl` (`http://localhost:5002`).
- Rutas (lazy `loadComponent`) a componentes mínimos "Próximamente":
  `''`→dashboard (dentro del layout), `inventory`, `reports`,
  `auth/signin`, `auth/signup` (fuera del layout), `**`→ `not-found`.

## Criterios de aceptación

- [ ] `docker compose -f deploy/docker-compose.yml up -d` → 4 servicios `healthy`.
- [ ] `curl localhost:5001/health` y `curl localhost:5002/health` → `200`.
- [ ] `/swagger` carga en 5001 y 5002.
- [ ] `docker compose run --rm backend-auth dotnet build` y `... backend-business
      dotnet build` → sin warnings ni errores.
- [ ] `docker compose run --rm backend-auth dotnet test` y `... backend-business` →
      verde.
- [ ] `docker compose run --rm frontend npm run build` y `npm run lint` → OK.
- [ ] `http://localhost:4200` muestra el shell: sidebar colapsa/expande, overlay en
      móvil (`≤992px`), topbar fijo; rutas placeholder navegables; comparación visual
      contra `requirements/inapp/src/index.html` (colores `#E66239`, Poppins, iconos
      `ti`).
- [ ] `git status` limpio de secretos; `.env` no versionado.

## Pruebas

Solo humo: builds, health checks, `up` limpio desde cero, y la comparación visual del
shell. Sin tests de dominio (no hay dominio).

## Ramas/PR

Un solo PR: `feat/fase-00-andamiaje`. Commits separados: `docs:` (este roadmap) y
`chore:`/`feat:` (andamiaje).
