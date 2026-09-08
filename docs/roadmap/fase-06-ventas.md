# Fase 6 — Ventas

## Objetivo y alcance

El corazón operativo: tomar pedidos, cobrarlos y descontar inventario.

- `orders` con **canal explícito** (`MESA`/`BARRA`/`TAKEAWAY`/`DELIVERY`, ORD-05) y
  coherencia canal ↔ mesa ↔ sesión (ORD-06, DOM-01, DOM-02, ORD-03, ORD-04).
- `order_items` con `unit_price` capturado al momento; cálculo de `total_amount` con
  impuestos (`menu_item_taxes`) y descuentos (`order_discounts`).
- `payments` múltiples por pedido, incluida **división de cuenta** (§5.2); pago con
  gift card genera `gift_card_transactions`.
- **Descuento de stock por receta** (§7.5, §11.5): al alcanzar el evento de dominio
  elegido, expandir `order_items` por `recipe_items`, agrupar por ingrediente y
  postear movimientos `SALE` mediante el `InventoryLedger` de Fase 3 — **idempotente**
  (DOM-07) y con **reversa** si el pedido se cancela.
- Barra: flujo §11.7, **sin** gestión de cupo (BAR-03, DOM-09).

## Dependencias

Fase 4 (menú, recetas, impuestos) y Fase 5 (mesas/sesiones para canal `MESA`).
`gift_card_transactions` se crea aquí; la emisión/saldo de la tarjeta es Fase 8.

### Decisiones bloqueantes

1. **Catálogo `orders.status` y `payments.status`** aprobados (borrador transversal
   §2). Qué `status` cuentan como **venta efectiva** (MET-01).
2. **Evento de dominio que dispara el movimiento `SALE`** (§7.5): p. ej. al cerrar el
   pedido, al marcar "preparado", etc. **Política de reversa** ante cancelación.
3. Impuestos aditivos vs inclusivos (viene de Fase 4 decisión 1); **redondeo**
   monetario (medio-arriba, 2 decimales).
4. `discounts.type` (`PERCENTAGE`/`FIXED_AMOUNT`) y cómo se calcula
   `order_discounts.applied_amount`; ¿varios descuentos por pedido?
5. `payments.payment_method` catálogo; ¿propina?

## Backend

- Entidades: `Order` (agregado raíz con `OrderItem`s), `Payment`, `Discount`,
  `OrderDiscount`, `GiftCardTransaction`.
- Reglas del agregado `Order`:
  - Fijar `channel` en la creación; aplicar el `CHECK` cruzado en el dominio también
    (ORD-06): `MESA` ⇒ `table_id` obligatorio; otro canal ⇒ `table_id` y
    `table_session_id` nulos.
  - `DOM-01`: `branch_id` = sucursal de la mesa y del empleado (ORD-04).
  - `DOM-02`/`ORD-03`: si hay `table_session_id`, la sesión es de `table_id`.
  - Recalcular `total_amount` = Σ(`order_items`) + impuestos − descuentos, con
    redondeo definido.
- Casos de uso:
  - `CreateOrder(channel, …)`, `AddItem`, `UpdateItem`, `RemoveItem`.
  - `ApplyDiscount`, `RemoveDiscount`.
  - `RegisterPayment` (varios; valida que Σ pagos ≤ total; permite split); pago
    `GIFT_CARD` descuenta saldo y crea `GiftCardTransaction`.
  - `CloseOrder` / `CancelOrder` — según decisión 2, dispara o revierte los `SALE`.
  - `PostSaleConsumption(orderId)` — servicio que expande recetas y llama al
    `InventoryLedger` (idempotente por `reference_type='ORDER'`, `reference_id=order.id`).
- Endpoints `/api/v1/orders` (+ `/{id}/items`, `/{id}/discounts`, `/{id}/payments`,
  `/{id}/close`, `/{id}/cancel`), `/discounts`.
- Consulta: `GET /orders?channel=&status=&sessionId=` (soporta cuenta por sesión y
  §12.6 pedidos de barra abiertos).

## Frontend

| Ruta | Contenido | Cómo construirla |
|---|---|---|
| `pos` | **Toma de pedido**: elegir canal (mesa desde el tablero / barra / para llevar), agregar platos del menú, notas, ver subtotal/impuestos/descuentos/total | rejilla de menú (`card`), panel de cuenta (`list-group`), botones grandes; reutiliza `LayoutComponent` |
| `pos/order/:id` | Cuenta del pedido: ítems, descuentos, **cobro** (varios pagos, split, gift card), cerrar | `list-group` + modal de pago |
| `sales/discounts` | CRUD de descuentos (tipo, valor, vigencia) | tabla + form |
| `sales/orders` | Historial de pedidos con filtros (canal, estado, fecha) | `inventory.html` + badges de estado |

- Desde el **tablero de salón** (Fase 5): "Abrir cuenta" en una mesa ocupada lleva a
  `pos` con `channel=MESA` y la sesión asociada.
- Menú lateral: grupo "Ventas" (POS, Pedidos, Descuentos).

## Criterios de aceptación

- [ ] Todo pedido se crea con `channel` explícito (§15).
- [ ] `channel ≠ MESA` ⇒ `table_id` y `table_session_id` siempre nulos (§15, ORD-06);
      `channel = MESA` ⇒ `table_id` obligatorio y, si hay sesión, coherente
      (DOM-02/ORD-03).
- [ ] `branch_id` del pedido coincide con la mesa y con el empleado (DOM-01/ORD-04).
- [ ] `total_amount` = ítems + impuestos − descuentos, con el redondeo acordado;
      recalculado ante cada cambio.
- [ ] Varios `payments` por pedido y varias `orders` por `table_session` (split,
      §5.2).
- [ ] El descuento de stock ocurre **una sola vez** por pedido (DOM-07); cancelar un
      pedido ya contabilizado genera reversa (§7.5).
- [ ] Ningún camino valida cupo/capacidad de barra (BAR-03, DOM-09, §15).
- [ ] `orders.status` de "venta efectiva" definido y usado por las consultas (MET-01).

## Pruebas

- Unit: agregado `Order` (coherencias de canal, recálculo de total, redondeo),
  cálculo de descuentos, expansión de recetas.
- Integración: ciclo completo mesa (§11.1), identificado (§11.2), barra (§11.7);
  venta + consumo de ingredientes (§11.5); cancelación con reversa; split de pago;
  pago con gift card.
- Frontend: POS (agregar/quitar ítems, totales en vivo), cobro con split.

## Ramas/PR (fase grande — cortar en 4)

1. `feat/fase-06a-pedidos` — agregado `Order`, ítems, coherencias de canal, total.
2. `feat/fase-06b-descuentos-pagos` — descuentos, pagos múltiples, gift card como
   medio de pago.
3. `feat/fase-06c-consumo-inventario` — `PostSaleConsumption` + reversa.
4. `feat/fase-06d-pos-frontend` — POS, cuenta, cobro, historial.
