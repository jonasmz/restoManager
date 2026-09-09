# Fase 5 — Salón

## Objetivo y alcance

Gestión del salón: mesas, ocupación real y reservas.

- CRUD de `tables` (`operational_status` ∈ `ACTIVE`/`CLEANING`/`OUT_OF_SERVICE`,
  `UNIQUE(branch_id, number)`).
- Apertura y cierre de `table_sessions` (SES-01…04, DOM-03).
- **Estado de uso derivado** `AVAILABLE`/`RESERVED`/`OCCUPIED` con
  `tableDisplayStatus(table, now)` (spec §4.2 y Anexo B) — **nunca** persistido
  (TBL-01).
- CRUD de `reservations` (RSV-01…04) y derivación de `RESERVED` dentro de la ventana
  configurable (TBL-02).
- **Vista de salón** nueva (no existe en el template).

## Dependencias

Fase 2 (sucursales, empleados). Fase 8 (clientes) es lo ideal para reservas —el
esquema exige `customer_id NOT NULL` en `reservations`—; si Fase 5 va antes que la 8,
se usa un mínimo alta-rápida de cliente o se difiere la parte de reservas a un PR
posterior a Fase 8.

### Decisiones bloqueantes — RESUELTAS (2026-09-09)

1. **Ventana de reserva (TBL-02): 30 min antes / 30 min después** de
   `reservation_time`. Configurable en `Salon:ReservationWindow`
   (`BeforeMinutes`/`AfterMinutes`). Fuera de la ventana la mesa vuelve a
   `AVAILABLE` aunque la reserva siga `CONFIRMED` hasta que se marque `NO_SHOW`.
2. **Catálogo `reservations.status`** confirmado según transversal §2:
   `PENDING → CONFIRMED → SEATED`; `PENDING/CONFIRMED → CANCELLED`;
   `CONFIRMED → NO_SHOW`. `RESERVED` es derivado (solo `CONFIRMED` en ventana).
3. **Regla de conflicto**: al crear una reserva se rechaza (`409
   dining.reservation_overlap`) si ya hay otra reserva activa
   (`PENDING`/`CONFIRMED`/`SEATED`) para la misma mesa cuyo horario cae dentro de
   ±(antes+después) del nuevo `reservation_time`. Una sesión abierta **no** impide
   crear una reserva futura; `SeatReservation` sí exige mesa `ACTIVE` sin sesión
   abierta.
4. **Clientes**: `reservations.customer_id` es `NOT NULL` y la Fase 8 aún no está;
   Fase 5 añade un **alta rápida de cliente** (`POST /api/v1/customers`, nombre +
   apellido + teléfono + email opcional). La Fase 8 se hace dueña del módulo.

## Backend

- Entidades: `Table`, `TableSession`, `Reservation`.
- Invariantes de aplicación:
  - `OpenSession`: mesa `ACTIVE` (SES-02, DOM-03), sin sesión abierta (SES-01 — la BD
    lo respalda con el índice único parcial), `guest_count > 0` (SES-04).
  - `CloseSession`: `closed_at >= opened_at` (SES-03), fija `closed_at = now`.
  - `SetOperationalStatus`: `ACTIVE ↔ CLEANING ↔ OUT_OF_SERVICE`; no permitir
    `CLEANING`/`OUT_OF_SERVICE` con sesión abierta.
  - `Reservation`: `RSV-01`/`DOM-04` (misma sucursal que la mesa); `party_size > 0`;
    barra no seleccionable (RSV-04, estructural).
- Servicio `ITableStatusResolver.Resolve(tableId, now)` que implementa el Anexo B
  (precedencia: `OUT_OF_SERVICE` → `CLEANING` → sesión abierta ⇒ `OCCUPIED` →
  reserva confirmada en ventana ⇒ `RESERVED` → `AVAILABLE`). **Solo lectura.**
- Casos de uso: `CreateTable`, `SetTableStatus`, `OpenSession`, `CloseSession`,
  `CreateReservation`, `ConfirmReservation`, `CancelReservation`, `MarkNoShow`,
  `SeatReservation` (abre sesión desde una reserva, flujo §11.3).
- Endpoints `/api/v1/tables` (+ `/{id}/status`, `/{id}/sessions`,
  `/{id}/sessions/{sid}/close`), `/floor` (estado derivado de todas las mesas de la
  sucursal), `/reservations`.

## Frontend

| Ruta | Contenido | Cómo construirla |
|---|---|---|
| `floor` | **Tablero de salón**: rejilla de mesas, color por estado derivado (`AVAILABLE`/`RESERVED`/`OCCUPIED`/`CLEANING`/`OUT_OF_SERVICE`), click abre acciones (abrir/cerrar sesión, cambiar estado) | `card` + `badge` + rejilla `row`/`col`; modal de acciones; polling o refresco manual |
| `tables` | CRUD de mesas (número, capacidad, estado operativo) | `inventory.html` + form |
| `reservations` | Agenda de reservas del día/semana + alta | tabla + form; selector de cliente |

- Menú lateral: grupo "Salón" (Tablero, Mesas, Reservas).

## Criterios de aceptación

- [ ] `tables.operational_status` solo toma los 3 valores del `CHECK`; el estado de
      uso **no** se guarda (TBL-01).
- [ ] No se puede abrir una segunda sesión en una mesa con sesión abierta (SES-01),
      ni abrir sesión en mesa `CLEANING`/`OUT_OF_SERVICE` (SES-02/DOM-03).
- [ ] `tableDisplayStatus` respeta exactamente la precedencia del Anexo B.
- [ ] Una reserva `CONFIRMED` dentro de la ventana muestra la mesa `RESERVED`; al
      abrir la sesión pasa a `OCCUPIED` (§11.3, RSV-02).
- [ ] El tablero refleja los 5 estados con colores consistentes con el template.
- [ ] La UI de reservas nunca ofrece "la barra" (§15, RSV-04).

## Pruebas

- Unit: `TableStatusResolver` (todos los caminos del Anexo B), invariantes de sesión.
- Integración: abrir/cerrar sesión, conflicto de sesión, ciclo de reserva, `seat`.
- Frontend: tablero (render por estado), acciones, agenda de reservas.

## Ramas/PR (cortar en 2–3)

1. `feat/fase-05a-mesas-sesiones` — mesas, sesiones, `TableStatusResolver`, `/floor`.
2. `feat/fase-05b-reservas` — reservas y derivación de `RESERVED`.
3. `feat/fase-05c-salon-frontend` — tablero, CRUD mesas, agenda.
