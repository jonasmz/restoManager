# Flujo de trabajo

Documento central: cómo aborda un agente cualquier tarea en este repositorio.
Pensado para ejecutarse en orden.

## 1. Leer el contexto

Antes de escribir código:

1. Lee [`../AGENTS.md`](../AGENTS.md) (reglas de oro).
2. Identifica **qué fase del roadmap** cubre la tarea: abre [`roadmap.md`](roadmap.md),
   luego `roadmap/fase-XX-*.md` de esa fase y [`roadmap/transversales.md`](roadmap/transversales.md).
   Resuelve con el usuario las "decisiones bloqueantes" de la fase antes de implementar.
3. Abre el/los documento(s) de `docs/` de la capa que vas a tocar:
   - Cambio en API/dominio/persistencia → [`backend.md`](backend.md) y, si toca
     usuarios/roles/tokens, [`auth.md`](auth.md).
   - Cambio en la interfaz → [`frontend.md`](frontend.md).
   - Cambio en contenedores/orquestación → [`docker.md`](docker.md).
4. Lee la sección relevante del dominio en `requirements/`:
   - `requirements/restaurant_schema_specifications.md` — reglas funcionales, con
     identificadores normativos (`CUS-01`, `TBL-02`, `ORD-06`, `INV-05`, `DOM-07`…).
   - `requirements/restaurant_schema.sql` — DDL real (tablas, CHECKs, índice único
     parcial de sesión abierta).
   - Para UI: la página equivalente en `requirements/inapp/src/*.html` y los
     parciales SCSS en `requirements/inapp/src/assets/scss/`.

> El spec usa **DEBE / DEBERÍA / PUEDE** (MUST / SHOULD / MAY). Trátalos como
> normativos: un "DEBE" no es negociable sin preguntar.

## 2. Planificar el cambio

Responde para ti mismo, antes de tocar nada:

- ¿Qué **capa hexagonal** se ve afectada? (Domain / Application / Infrastructure /
  Api — ver [`arquitectura.md`](arquitectura.md)). Un cambio bien planteado
  normalmente entra por Application y solo baja a Infrastructure si hace falta un
  adaptador nuevo.
- ¿Cambia el **contrato** de la API (request/response, códigos, rutas)? → es
  crucial, se pregunta.
- ¿Impacta el **modelo de datos**? → ¿necesita migración EF Core? ¿respeta el DDL de
  `restaurant_schema.sql`?
- ¿Toca **varias tablas en una operación**? → debe ir en **una sola transacción**
  (spec §1.1, `INV-05`).
- En frontend: ¿qué página del template es el referente visual? ¿qué componentes
  del shell reutilizo?

## 3. Clasificar las dudas: preguntar vs. decidir

| Situación | Acción |
|---|---|
| Contrato de API (rutas, DTOs, códigos de error, paginación) | **Preguntar** |
| Forma de una entidad nueva o cambio de columnas | **Preguntar** |
| Regla de negocio ambigua o no cubierta por el spec (p. ej. ventana de reserva `TBL-02`, catálogo de `orders.status` `MET-01`, momento exacto de descуento de stock §7.5) | **Preguntar** |
| Autorización: qué rol puede hacer qué | **Preguntar** |
| Añadir una dependencia / paquete NuGet o npm nuevo | **Preguntar** |
| Cualquier desviación respecto al template `inapp/` o al spec | **Preguntar** |
| Nombres de variables/métodos locales, estructura interna de una clase o componente | **Decidir y anotar en el commit** |
| Orden de campos en un formulario, textos y etiquetas de UI, formato de fechas | **Decidir y anotar** |
| Refactor sin cambio de comportamiento observable | **Decidir y anotar** |
| Añadir tests | **Hacer siempre, no hace falta preguntar** |

Cuando preguntes, hazlo con opciones concretas y una recomendación, no en abstracto.

## 4. Implementar dentro del contenedor

**Nunca** ejecutes `dotnet`, `npm`, `ng` o `psql` en el host. Usa el contenedor:

```bash
# Backend (negocio)
docker compose exec backend-business dotnet build
docker compose run --rm backend-business dotnet ef migrations add <Nombre> \
  --project src/RestoManager.Business.Infrastructure \
  --startup-project src/RestoManager.Business.Api
docker compose exec backend-business dotnet test

# Backend (auth)
docker compose exec backend-auth dotnet test

# Frontend
docker compose run --rm frontend npm install <paquete>   # solo tras aprobación
docker compose exec frontend npm run lint
docker compose exec frontend npm test

# Base de datos
docker compose exec postgres psql -U postgres -d resto_business
```

Detalles y puertos en [`docker.md`](docker.md).

## 5. Verificar

| Tipo de cambio | Verificación mínima |
|---|---|
| Dominio / Application | Unit tests nuevos que cubran la regla; `dotnet test` verde. |
| Endpoint | Test de integración (Postgres en contenedor) + revisar OpenAPI/Swagger. |
| Migración EF Core | `dotnet ef database update` en contenedor; revisar el SQL generado contra `restaurant_schema.sql`. |
| Pantalla Angular | `npm run lint` + `npm test`; **comparación visual** contra la página equivalente de `requirements/inapp/` (layout, colores, espaciado, iconos). |
| Cambio en contenedores | `docker compose up` limpio desde cero; healthchecks en verde. |

## 6. Entregar

El flujo completo (ramas, commits, Pull Request, credenciales) está en
[`git-github.md`](git-github.md). Resumen:

- **Una feature = una rama = un PR.** Se parte de `main` actualizada
  (`git switch main && git pull --ff-only`) y se crea
  `<prefijo>/<area>-<resumen-corto>` — prefijos `feat/`, `fix/`, `refactor/`,
  `docs/`, `chore/`, `test/` (p. ej. `feat/inventory-movimientos`,
  `docs/backend-testing`).
- **Commits**: [Conventional Commits](https://www.conventionalcommits.org), mensaje
  en español, con los *trailers* obligatorios `Co-Authored-By:` y `Claude-Session:`.
- **Integración a `main` solo por Pull Request.** Nunca merge local ni push directo
  a `main`. El merge del PR lo hace una persona.
- **Descripción del PR** (plantilla en `git-github.md`): qué se hizo · contra qué
  requisito del spec · **decisiones menores tomadas** sin consultar · cómo verificar.

## Definición de Hecho por tipo de cambio

**Endpoint nuevo**
- [ ] Caso de uso en Application con su(s) test(s).
- [ ] Puerto(s) definidos en Domain, adaptador(es) en Infrastructure.
- [ ] Validación de entrada y `ProblemDetails` en error.
- [ ] Autorización aplicada (rol/policy) — confirmada con el usuario.
- [ ] Invariantes de dominio aplicables verificados (`DOM-01…DOM-10`).
- [ ] Documentado en OpenAPI; test de integración.

**Entidad / cambio de esquema**
- [ ] Mapeo EF Core coherente con `restaurant_schema.sql` (snake_case, identity).
- [ ] Migración generada y revisada; `database update` aplica limpio.
- [ ] Sin lógica en la BD (sin triggers); las reglas viven en Application.

**Pantalla / feature de frontend**
- [ ] Coincide visualmente con la página de `requirements/inapp/`.
- [ ] Componente standalone, `OnPush`, señales; sin `any`.
- [ ] Estados de carga/error/vacío contemplados.
- [ ] Reutiliza el shell (`LayoutComponent`) y los componentes compartidos.
- [ ] `lint` y tests en verde.

**Cambio de infraestructura Docker**
- [ ] `docker compose up` funciona desde cero, sin pasos manuales en el host.
- [ ] Secretos vía `.env` / variables, nunca en el repo.
