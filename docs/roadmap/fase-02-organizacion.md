# Fase 2 — Organización

## Objetivo y alcance

Administrar la estructura de la empresa y habilitar el **branch scoping** que usa todo
el resto del sistema:

- CRUD de `restaurants`, `branches`, `departments`, `roles`, `employees`, `shifts`,
  `employee_leaves`.
- **Selector de sucursal** global en el frontend y filtrado por sucursal en la
  Business API (regla transversal §1).
- Validación de `DOM-06` (empleado habilitado para la sucursal) reutilizable por
  todas las fases.
- **Datos semilla**: 1 restaurante, 2 sucursales, departamentos, roles del catálogo
  (Fase 1), y empleados demo vinculados a usuarios de Identity.

## Dependencias

Fase 1. Decisiones bloqueantes: confirmar cómo viaja la sucursal activa en cada
request (header `X-Branch-Id` recomendado) y el formato del claim `branch_ids`.

## Backend

- **Migración baseline** de `resto_business`: la primera migración reproduce
  `requirements/restaurant_schema.sql` completo (todas las tablas, no solo las de esta
  fase) — ver transversal §7. Validar con `dotnet ef migrations script` contra el
  `.sql`.
- Entidades de dominio de esta fase con sus invariantes:
  - `Branch` pertenece a `Restaurant`; `Department`, `Employee`, `Shift` cuelgan de
    `Branch`. `Employee` referencia `Department` + `Role`.
  - `EmployeeLeave`: `end_date >= start_date`; `status`/`leave_type` del catálogo
    (transversal §2).
  - `Shift`: `end_time > start_time`; `scheduled_hours` coherente.
- Puertos: `IRestaurantRepository`, `IBranchRepository`, `IEmployeeRepository`, …
- Servicio transversal `IBranchAccessPolicy.EnsureCanOperate(branchId)` → `DOM-06`,
  usado por el pipeline de todos los casos de uso con sucursal.
- Casos de uso: `Create/Update/Deactivate` por entidad + consultas paginadas.
- Endpoints `/api/v1/restaurants`, `/branches`, `/departments`, `/roles`,
  `/employees`, `/employees/{id}/shifts`, `/employees/{id}/leaves`. Autorización según
  matriz de Fase 1 (típicamente rol administrador/gerente).
- Semilla idempotente.

## Frontend

Pantallas de administración (patrón tabla + formulario del template):

| Ruta | Contenido | Referencia visual |
|---|---|---|
| `org/restaurant` | Datos del restaurante (form) | `create-product.html` |
| `org/branches` | Lista + alta/edición de sucursales | `inventory.html` + form |
| `org/departments` | Lista por sucursal | ídem |
| `org/roles` | Lista de roles y tarifa horaria | ídem |
| `org/employees` | Lista + ficha de empleado (departamento, rol, contratación) | ídem |
| `org/employees/:id` | Turnos y ausencias del empleado | `list-group` + modal |

- `core/branch/branch-context.service`: señal `activeBranch`, persistida en
  `localStorage`, enviada en cada request (interceptor). Selector en el topbar del
  `LayoutComponent`.
- Menú lateral: añadir grupo "Organización".

## Criterios de aceptación

- [x] **Migración baseline `InitialSchema` hecha (PR #7):** las 35 tablas de
      `restaurant_schema.sql` modeladas en EF Core (snake_case, identity, los 9
      `CHECK`, los `UNIQUE`, el índice único parcial de sesión abierta, todas las
      FK sin cascada). Equivalencias cosméticas de Postgres: `varchar(n)` ↔
      `character varying(n)`, `decimal` ↔ `numeric`, constraints con nombres
      `fk_*`/`ck_*`/`ux_*`. `timestamp` (sin zona) ↔ `DateTime` con
      `Kind=Unspecified` (ver `SystemClock`). Las 31 entidades no-Fase-3 son
      "portadoras de esquema" (POCO); cada fase les añade comportamiento.
- [ ] CRUD completo de las 7 entidades vía API, paginado y con `ProblemDetails`.
- [ ] Toda consulta de datos de sucursal exige sucursal activa y valida `DOM-06`
      (401/403 correctos).
- [ ] El selector de sucursal cambia el contexto y las listas reflejan la sucursal
      elegida.
- [ ] Semilla reproducible: `docker compose run --rm backend-business dotnet run
      --seed` (o migración de datos) deja el sistema usable para Fase 3+.
- [ ] Un empleado no habilitado para la sucursal B no puede operar sobre B.

## Pruebas

- Unit: invariantes (`EmployeeLeave`, `Shift`), `BranchAccessPolicy`.
- Integración: CRUD + filtrado por sucursal + 403 por `DOM-06`.
- Frontend: `BranchContextService`, listados, formularios con validación.

## Ramas/PR (fase grande — cortar en 3)

1. `feat/fase-02a-baseline-migracion` — migración baseline + entidades + repos + seed
   vacío.
2. `feat/fase-02b-org-backend` — casos de uso, endpoints, `BranchAccessPolicy`, seed.
3. `feat/fase-02c-org-frontend` — selector de sucursal + pantallas de administración.
