# Fase 7 — Delivery

## Objetivo y alcance

Reparto a domicilio como extensión del canal `DELIVERY`:

- `delivery_drivers` (vinculados a `employees`).
- `deliveries` (dirección, hora estimada, hora real nullable, estado).
- Garantizar **DOM-08**: `orders.channel = 'DELIVERY'` ⇔ existe fila en `deliveries`
  para ese `order_id`, y viceversa. Application-driven (sin FK inversa ni trigger).

## Dependencias

Fase 6 (pedidos con canal). Fase 2 (empleados, para repartidores).

### Decisiones bloqueantes — RESUELTAS (2026-09-10)

1. **`deliveries.status`** = `PENDING → ASSIGNED → IN_TRANSIT → DELIVERED`;
   `ASSIGNED/IN_TRANSIT → FAILED`; `PENDING/ASSIGNED/FAILED → ASSIGNED` (reasignar,
   incl. reintento tras `FAILED`); `PENDING/ASSIGNED/IN_TRANSIT/FAILED → CANCELLED`.
   Nace `PENDING` **con repartidor** (`deliveries.driver_id` es NOT NULL; no hay
   estado "sin asignar" representable). `actual_time` solo se informa al pasar a
   `DELIVERED` o `FAILED`.
2. **La fila `deliveries` se crea en la misma transacción que el pedido
   `DELIVERY`**, ya con repartidor: `CreateOrder(channel=DELIVERY)` exige
   `deliveryAddress` + `estimatedTime` + `driverId`. DOM-08 se cumple siempre, sin
   ventana de incoherencia. `CloseOrder` tiene además una red de seguridad
   (`sales.delivery_missing` si faltara la fila).
3. **`delivery_drivers.vehicle_type`** = `MOTORCYCLE` | `BICYCLE` | `CAR` | `ON_FOOT`.
4. **Cancelar una entrega cancela el pedido** en la misma transacción: pagos
   `CONFIRMED` → `REFUNDED` y reversa del stock si el pedido estaba `PAID`. Se
   rechaza (`delivery.order_closed`) si el pedido ya está `CLOSED`.

## Backend

- Entidades: `DeliveryDriver`, `Delivery`.
- Regla de creación de pedido `DELIVERY` (extiende Fase 6): el caso de uso
  `CreateOrder` con `channel = DELIVERY` **exige** los datos de entrega y crea la
  `Delivery` en la **misma transacción** (DOM-08). No se puede cerrar un pedido
  `DELIVERY` sin `Delivery`.
- Casos de uso: `AssignDriver`, `MarkInTransit`, `MarkDelivered` (informa
  `actual_time`), `MarkFailed`, `CancelDelivery`. Cancelar la `Delivery` implica
  cancelar/regularizar el pedido (coherencia DOM-08).
- Verificación de invariante: un job/consulta de salud que detecte pedidos `DELIVERY`
  sin `deliveries` y viceversa (no debería ocurrir; sirve de red).
- Endpoints `/api/v1/delivery-drivers`, `/deliveries` (+ `/{id}/assign`,
  `/{id}/status`), y `/orders` con `channel=DELIVERY` ya soportado desde Fase 6.

## Frontend

| Ruta | Contenido | Cómo construirla |
|---|---|---|
| `delivery/board` | **Despacho**: pedidos `DELIVERY` por estado (`PENDING`/`ASSIGNED`/`IN_TRANSIT`/…), asignar repartidor, avanzar estado | columnas tipo kanban con `card` + `badge`; modal de asignación |
| `delivery/drivers` | CRUD de repartidores (empleado, vehículo, patente) | tabla + form |

- En el **POS** (Fase 6): al elegir canal `DELIVERY`, el formulario pide dirección y
  hora estimada antes de permitir crear el pedido.
- Menú lateral: grupo "Delivery".

## Criterios de aceptación

- [x] No se puede crear/cerrar un pedido `DELIVERY` sin su fila en `deliveries`
      (DOM-08, §15). *(7a)*
- [x] No existe fila en `deliveries` cuyo `order.channel ≠ DELIVERY` (solo `CreateOrder`
      con canal DELIVERY crea entregas). *(7a)*
- [x] `actual_time` se informa solo al pasar a `DELIVERED`/`FAILED`; es `NULL` antes. *(7a)*
- [x] Asignar repartidor y avanzar estado siguen las transiciones del catálogo. *(7a)*
- [ ] El tablero de despacho refleja los estados con colores del template. *(7b)*

## Pruebas

- Unit: regla DOM-08 en `CreateOrder(DELIVERY)`; transiciones de estado de entrega.
- Integración: flujo completo pedido `DELIVERY` → asignación → entrega; intento de
  crear `DELIVERY` sin datos de entrega (rechazado); cancelación coherente.
- Frontend: tablero de despacho, CRUD de repartidores, formulario de entrega en POS.

## Ramas/PR (cortar en 2)

1. `feat/fase-07a-delivery-backend` — entidades, DOM-08 en creación, transiciones. **Hecha (PR #20).**
2. `feat/fase-07b-delivery-frontend` — despacho + repartidores + form en POS.

### Estado 7a (backend)

- Hexágono `Delivery`: enums `VehicleType` (MOTORCYCLE/BICYCLE/CAR/ON_FOOT) y
  `DeliveryStatus` con `ToDbValue`/`FromDbValue`/`TryFromDbValue`. `DeliveryDriver`
  rico (invariantes). `Delivery` rico: `Create` → `PENDING`; `AssignDriver`
  (PENDING/ASSIGNED/FAILED), `MarkInTransit`, `MarkDelivered`/`MarkFailed` (fijan
  `actual_time`), `Cancel`.
- **DOM-08 en `CreateOrder`**: `channel=DELIVERY` exige `deliveryAddress` +
  `estimatedTime` + `driverId`; crea el pedido y la `Delivery` en un
  `ExecuteInTransactionAsync`. Los demás canales rechazan esos campos (422).
  `CloseOrderHandler` verifica que exista la entrega (`sales.delivery_missing`).
- Casos de uso (`Application/Deliveries/`): `SaveDriver`/`ListDrivers`/`GetDriver`;
  `ListDeliveries` (tablero por sucursal, join con `orders`, filtro por estado),
  `GetDelivery`, `AssignDriver`, `AdvanceDelivery` (in-transit/delivered/failed),
  `CancelDelivery` (cascada a `CancelOrder` + reversa de stock + refund, misma tx;
  409 si el pedido está `CLOSED`).
- `IDeliveryDriverRepository` + `IDeliveryRepository` (con `GetWithOrderAsync` y
  `ListForBranchAsync` que unen con `orders`).
- Endpoints: `/api/v1/delivery-drivers` (GET/POST/PUT, policy `OrgStaff`),
  `/api/v1/deliveries` (GET `?status=`, GET `/{id}`, POST `/{id}/{assign|in-transit|
  delivered|failed|cancel}`, policy `DeliveryAccess` = ADMIN/BRANCH_MANAGER/WAITER).
  `POST /api/v1/orders` acepta ahora los campos de entrega.
- **Sin migración**: enums → mismos strings/longitudes del DDL. `migrations add` en
  seco da `Up()` vacío.
- Seeder: un repartidor demo (`DEMO-001`, MOTORCYCLE) sobre un empleado existente.
- 14 tests unit nuevos (107 total). Verificado e2e vs compose: crear pedido DELIVERY
  (422 sin datos / 404 repartidor inexistente), tablero, `PENDING→ASSIGNED→
  IN_TRANSIT→DELIVERED` con `actual_time`, doble entrega 409, cancelar entrega →
  pedido `CANCELLED` + pago `REFUNDED` + reversa `ADJUSTMENT +0.60`, idempotencia,
  cancelar entrega de pedido `CLOSED` → 409.
- Tras mergear #20: falta 7b (frontend). Siguiente fase: 8 (Clientes y fidelización).
