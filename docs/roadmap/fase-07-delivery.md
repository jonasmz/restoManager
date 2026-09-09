# Fase 7 — Delivery

## Objetivo y alcance

Reparto a domicilio como extensión del canal `DELIVERY`:

- `delivery_drivers` (vinculados a `employees`).
- `deliveries` (dirección, hora estimada, hora real nullable, estado).
- Garantizar **DOM-08**: `orders.channel = 'DELIVERY'` ⇔ existe fila en `deliveries`
  para ese `order_id`, y viceversa. Application-driven (sin FK inversa ni trigger).

## Dependencias

Fase 6 (pedidos con canal). Fase 2 (empleados, para repartidores).

### Decisiones bloqueantes

1. Catálogo `deliveries.status` (borrador transversal §2).
2. ¿Se crea la `delivery` en el mismo acto que el pedido `DELIVERY`, o el pedido
   `DELIVERY` nace "sin asignar" y la `delivery` se completa luego? (afecta cómo se
   hace cumplir DOM-08).
3. `delivery_drivers.vehicle_type` catálogo.

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

- [ ] No se puede crear/cerrar un pedido `DELIVERY` sin su fila en `deliveries`
      (DOM-08, §15).
- [ ] No existe fila en `deliveries` cuyo `order.channel ≠ DELIVERY`.
- [ ] `actual_time` se informa solo al pasar a `DELIVERED`/`FAILED`; es `NULL` antes.
- [ ] Asignar repartidor y avanzar estado siguen las transiciones del catálogo.
- [ ] El tablero de despacho refleja los estados con colores del template.

## Pruebas

- Unit: regla DOM-08 en `CreateOrder(DELIVERY)`; transiciones de estado de entrega.
- Integración: flujo completo pedido `DELIVERY` → asignación → entrega; intento de
  crear `DELIVERY` sin datos de entrega (rechazado); cancelación coherente.
- Frontend: tablero de despacho, CRUD de repartidores, formulario de entrega en POS.

## Ramas/PR (cortar en 2)

1. `feat/fase-07a-delivery-backend` — entidades, DOM-08 en creación, transiciones.
2. `feat/fase-07b-delivery-frontend` — despacho + repartidores + form en POS.
