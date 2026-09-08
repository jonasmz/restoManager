# AGENTS.md — Resto Manager

Guía de entrada para agentes de IA (y personas) que trabajan en este repositorio.
**Léela completa al inicio de cada sesión** y luego abre el/los documento(s) de
`docs/` que correspondan a la capa que vas a tocar.

## Qué es este proyecto

Sistema de gestión para un **restaurante multi-sucursal**: salón y mesas, barra,
pedidos por canal (mesa / barra / para llevar / delivery), clientes y fidelización,
ventas y pagos, e inventario por sucursal con trazabilidad. El dominio completo está
especificado en `requirements/` y es la **fuente de verdad**.

## Stack

| Capa | Tecnología |
|---|---|
| Backend | .NET 10 · ASP.NET Core Web API · EF Core · **arquitectura hexagonal** |
| Auth | API independiente con ASP.NET Core Identity · emite JWT firmados (clave asimétrica) + JWKS |
| Base de datos | PostgreSQL 17 (un contenedor, dos bases: `resto_identity`, `resto_business`) |
| Frontend | Angular 22 · Bootstrap 5.3 (SCSS) · Tabler Icons (webfont) · ApexCharts |
| Ejecución | Docker: un contenedor por tecnología. **Nada se instala en el host.** |

## Reglas de oro

1. **Todo corre en contenedores.** No instales SDKs, CLIs ni dependencias en el host
   (ni `dotnet`, ni `node`/`npm`, ni `psql`). Cada comando se ejecuta dentro de su
   contenedor vía `docker compose run/exec`. Ver [`docs/docker.md`](docs/docker.md).
2. **`requirements/` no se modifica.** Es la fuente de verdad del dominio
   (`restaurant_schema.sql`, `restaurant_schema_specifications.md`) y del diseño de UI
   (`requirements/inapp/`).
3. **El frontend reproduce fielmente el template** `requirements/inapp/`: mismo
   layout, colores (`$primary: #E66239`), tipografía (Poppins), iconos (`ti ti-*`) y
   componentes. No inventes un diseño nuevo ni mezcles otro framework CSS.
4. **Arquitectura hexagonal en el backend.** Respeta la regla de dependencias: el
   dominio no depende de nada; la infraestructura depende hacia adentro. Ver
   [`docs/arquitectura.md`](docs/arquitectura.md).
5. **Buenas prácticas idiomáticas** de cada tecnología (las de cada `docs/*.md`).
6. **La lógica de negocio vive en la aplicación, no en la base de datos.** El spec
   prohíbe triggers; la BD es la última barrera de consistencia, no la primera.
7. **Pregunta antes de implementar lo crucial; decide directamente lo menor.**
   - **Preguntar:** contrato de API, forma del modelo de datos, reglas de negocio
     ambiguas, seguridad/autorización, añadir una dependencia nueva, cualquier
     desviación del template o del spec.
   - **Decidir y anotar en el commit/PR:** nombres de variables locales, orden de
     campos, textos de UI, estructura interna de un componente, refactors sin cambio
     de comportamiento.
8. **Git por features.** Cada feature en su propia rama; la integración a `main` es
   **solo por Pull Request**. Nunca se mergea `main` localmente ni se hace push
   directo a `main`. El merge del PR lo hace una persona. Ver
   [`docs/git-github.md`](docs/git-github.md).
9. **Idioma:** documentación y comentarios en **español**; identificadores de código
   (clases, métodos, variables, tablas) en **inglés**.

## Índice de documentación

| Documento | Contenido |
|---|---|
| [`docs/flujo-de-trabajo.md`](docs/flujo-de-trabajo.md) | Cómo abordar cada tarea, paso a paso. Qué preguntar. Definición de Hecho. |
| [`docs/arquitectura.md`](docs/arquitectura.md) | Visión del sistema, hexagonal, layout del monorepo, mapa de dominio e invariantes. |
| [`docs/backend.md`](docs/backend.md) | .NET 10, EF Core, capas hexagonales, testing, convenciones. |
| [`docs/auth.md`](docs/auth.md) | Auth API con Identity, emisión/validación de JWT, JWKS, roles. |
| [`docs/frontend.md`](docs/frontend.md) | Angular 22, fidelidad al template, SCSS/Bootstrap, mapeo de pantallas. |
| [`docs/docker.md`](docs/docker.md) | Los tres contenedores, comandos contenedorizados, secretos. |
| [`docs/git-github.md`](docs/git-github.md) | Flujo por features, ramas, Pull Requests hacia `main`, credenciales. |

## Estado del repositorio

- Repo git inicializado, remoto `origin` → `https://github.com/jonasmz/restoManager.git`
  (rama troncal `main`, integración solo por PR).
- Solo existe `requirements/` + esta documentación. Todavía **no** hay soluciones
  .NET, proyecto Angular ni `docker-compose.yml`; se crearán en tareas posteriores,
  cada una en su rama y su PR, siguiendo estos documentos.
