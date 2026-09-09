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

### Decisiones bloqueantes — RESUELTAS (2026-09-09)

1. **`orders.status` = `OPEN` → `PAID` → `CLOSED`**; `OPEN`/`PAID` → `CANCELLED`.
   `PAID` = existe ≥1 `payment` `CONFIRMED` (distingue "cobrado" de "cerrado/
   archivado"). **Venta efectiva (MET-01) = `status IN (PAID, CLOSED)`.**
   `payments.status` = borrador transversal (`PENDING`/`CONFIRMED`/`FAILED`/
   `REFUNDED`); cuenta para métricas `CONFIRMED`.
2. **El movimiento `SALE` se dispara al registrar el primer `payment` `CONFIRMED`**
   del pedido (transición `OPEN → PAID`). `PostSaleConsumption` expande recetas,
   agrupa por ingrediente y postea vía `InventoryLedger` de Fase 3, **idempotente**
   por `reference_type='ORDER'` + `reference_id = order.id` (DOM-07): pagos
   posteriores (split) no vuelven a descontar. **Reversa:** si un pedido ya `PAID`
   se `CANCELLED`, se generan movimientos de reversa (`ADJUSTMENT` opuesto, nunca
   borrado); los `payments` `CONFIRMED` pasan a `REFUNDED`. Pedidos que nunca
   llegaron a `PAID` no tocan stock.
3. Impuestos **inclusivos** en `price` (Fase 4 decisión 1). **Redondeo monetario:
   medio-arriba (`MidpointRounding.AwayFromZero`), 2 decimales**, aplicado al total
   de cada línea y al total del pedido.
4. `discounts.type` = `PERCENTAGE` | `FIXED_AMOUNT`. `order_discounts.applied_amount`
   se calcula y **congela al aplicar**: `PERCENTAGE` = `round(subtotal * rate/100)`
   sobre el **subtotal de ítems**; `FIXED_AMOUNT` = el importe fijo (topado al
   subtotal restante). **Se permiten varios descuentos por pedido**; se suman.
   `total_amount = subtotal − Σ applied_amount` (impuestos ya incluidos), sin bajar
   de 0.
5. `payments.payment_method` = `CASH` | `CARD` | `TRANSFER` | `GIFT_CARD` | `OTHER`
   (borrador transversal). `GIFT_CARD` genera `gift_card_transactions`. **Sin
   propina** en Fase 6 (el esquema no tiene columna; se difiere).

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

- [x] Todo pedido se crea con `channel` explícito (§15). *(6a)*
- [x] `channel ≠ MESA` ⇒ `table_id` y `table_session_id` siempre nulos (§15, ORD-06);
      `channel = MESA` ⇒ `table_id` obligatorio y, si hay sesión, coherente
      (DOM-02/ORD-03). *(6a)*
- [x] `branch_id` del pedido coincide con la mesa y con el empleado (DOM-01/ORD-04). *(6a)*
- [x] `total_amount` = ítems + impuestos − descuentos, con el redondeo acordado;
      recalculado ante cada cambio. *(6a/6b)*
- [x] Varios `payments` por pedido y varias `orders` por `table_session` (split,
      §5.2). *(6b)*
- [x] El descuento de stock ocurre **una sola vez** por pedido (DOM-07); cancelar un
      pedido ya contabilizado genera reversa (§7.5). *(6c)*
- [x] Ningún camino valida cupo/capacidad de barra (BAR-03, DOM-09, §15).
- [~] `orders.status` de "venta efectiva" definido y usado por las consultas (MET-01).
      Definido (`PAID`/`CLOSED`) y filtrable en `GET /orders`; las métricas lo consumen en Fase 9.

## Pruebas

- Unit: agregado `Order` (coherencias de canal, recálculo de total, redondeo),
  cálculo de descuentos, expansión de recetas.
- Integración: ciclo completo mesa (§11.1), identificado (§11.2), barra (§11.7);
  venta + consumo de ingredientes (§11.5); cancelación con reversa; split de pago;
  pago con gift card.
- Frontend: POS (agregar/quitar ítems, totales en vivo), cobro con split.

## Ramas/PR (fase grande — cortar en 4)

1. `feat/fase-06a-pedidos` — agregado `Order`, ítems, coherencias de canal, total. **Hecha (PR #16).**
2. `feat/fase-06b-descuentos-pagos` — descuentos, pagos múltiples, gift card como
   medio de pago. **Hecha (PR #17).**
3. `feat/fase-06c-consumo-inventario` — `PostSaleConsumption` + reversa. **Hecha (PR #18).**
4. `feat/fase-06d-pos-frontend` — POS, cuenta, cobro, historial.

### Estado 6a (backend de pedidos)

- Agregado `Order` (`SalesEntities.cs`): enums `OrderChannel`/`OrderStatus` con
  `ToDbValue`/`FromDbValue`/`TryFromDbValue`; `Order.Create` aplica ORD-06 (canal ↔
  mesa ↔ sesión); `AddItem`/`UpdateItem`/`RemoveItem` solo con estado `OPEN`;
  `Recalculate(discountTotal)` con `Money.Round` (medio-arriba, 2 decimales), total
  ≥ 0. `OrderItem.LineTotal` derivado (no se persiste).
- `Money` (`Domain/Common/Money.cs`): redondeo monetario único del sistema.
- Casos de uso (`Application/Sales/Orders/OrderUseCases.cs`): `CreateOrder` (DOM-01/
  ORD-04 empleado↔sucursal, DOM-01 mesa↔sucursal, DOM-02/ORD-03 sesión↔mesa+abierta,
  cliente existe), `AddOrderItem` (captura `unit_price` del plato), `UpdateOrderItem`,
  `RemoveOrderItem`, `ListOrders` (filtros canal/estado/sesión/fecha, paginado),
  `GetOrder`.
- Endpoints `/api/v1/orders` (+ `/{id}/items(+/{itemId})`), policy `SalesAccess`
  (ADMIN, BRANCH_MANAGER, WAITER), sucursal por `X-Branch-Id`.
- **Sin migración**: enums mapean a los mismos strings/longitudes del DDL; colección
  hija con `DeleteBehavior.ClientCascade` (FK NO ACTION en BD); `LineTotal` ignorado.
  `migrations add` en seco → `Up()`/`Down()` vacíos.
- 15 tests unit nuevos (69 total). Verificado e2e vs compose: ciclo MESA
  (sesión→pedido→ítems→total 35.50→update 28.50→remove), BARRA/TAKEAWAY/DELIVERY,
  coherencias (MESA sin mesa 409, no-MESA con mesa/sesión 422, sesión de otra mesa
  409, empleado de otra sucursal 409), filtros de listado.
### Estado 6b (descuentos, pagos, cierre)

- Enums nuevos en `SalesEntities.cs`: `PaymentMethod`, `PaymentStatus`, `DiscountType`
  (todos con `ToDbValue`/`FromDbValue`). `Discount` pasa a entidad rica (invariantes de
  valor/porcentaje/rango, `IsActiveOn`, `ComputeApplied`). `Payment` y `OrderDiscount`
  ricos. `GiftCard.Redeem(orderId, amount, now)` descuenta saldo y devuelve el asiento.
- Agregado `Order` amplía: colecciones `Discounts` + `Payments`; derivados
  `ItemsSubtotal`/`DiscountTotal`/`ConfirmedPaid`/`Balance` (no se persisten).
  `ApplyDiscount` (vigencia, `UNIQUE(order_id,discount_id)`, tope para total ≥ 0),
  `RemoveDiscount`, `RegisterPayment` (Σ pagos ≤ total §5.2; el 1.º `CONFIRMED` pasa a
  `PAID`), `CloseOrder` (exige pago completo o total 0), `CancelOrder` (`OPEN`/`PAID` →
  `CANCELLED`, pagos `CONFIRMED` → `REFUNDED`).
- Casos de uso (`OrderUseCases.cs` + `DiscountUseCases.cs`): `ApplyOrderDiscount`,
  `RemoveOrderDiscount`, `RegisterPayment` (pago `GIFT_CARD` = pago + canje + asiento
  en 1 tx), `CloseOrder`, `CancelOrder`; catálogo `SaveDiscount`/`ListDiscounts`/
  `GetDiscount`. `OrderDto` amplía con subtotal/descuentos/pagos/saldo.
- Endpoints: `POST /orders/{id}/discounts`, `DELETE /orders/{id}/discounts/{discountId}`,
  `POST /orders/{id}/payments`, `POST /orders/{id}/close`, `POST /orders/{id}/cancel`;
  `/api/v1/discounts` (GET/POST/PUT) con policy `DiscountAccess` (ADMIN, BRANCH_MANAGER).
- **Sin migración**: enums → strings/longitudes del DDL; colecciones hijas
  `ClientCascade`; derivados ignorados. `migrations add` en seco → `Up()` vacío.
- Seeder: 2 descuentos demo (Happy Hour 10 %, Bono $5) + tarjeta regalo `GC-DEMO-0001`
  con saldo 100 (idempotente).
- 19 tests unit nuevos (88 total). Verificado e2e vs compose: descuentos (suma, tope a
  0, duplicado 409, vencido 409, quitar), pagos (split, `PAID` al 1.º, exceso 409),
  gift card (canje descuenta saldo + asiento −monto, saldo insuficiente 409, sin id
  422), cierre (subpagado 409 → completo 204), cancelación (pagos → `REFUNDED`, editar
  cancelado 409, idempotente).
### Estado 6c (descuento de stock por receta y reversa)

- `SaleConsumptionService` (`Application/Sales/Consumption/`):
  - `PostForOrderAsync(order)` — expande `order_items` por `recipe_items`, agrupa la
    cantidad por ingrediente (redondeo a 2 dec.) y postea `SALE` negativos vía el
    `InventoryLedger` de Fase 3. Idempotente por `reference_type='ORDER'` +
    `reference_id = order.id` (DOM-07).
  - `ReverseForOrderAsync(orderId)` — por cada `SALE` de esa referencia postea un
    `ADJUSTMENT` de signo opuesto con la misma referencia (nunca borra movimientos;
    idempotente ante reintentos de cancelación).
- **Evento**: `RegisterPaymentHandler` detecta la transición `OPEN → PAID` (primer
  pago `CONFIRMED`) y llama a `PostForOrderAsync` **en la misma transacción** que el
  pago (y el canje de gift card). `CancelOrderHandler`: si el pedido estaba `PAID`,
  llama a `ReverseForOrderAsync` en la misma transacción que la cancelación.
- `IInventoryMovementRepository.ListByReferenceAsync(type, id)` nuevo.
- **INV-03**: si el `SALE` dejaría un saldo negativo, el `InventoryLedger` lanza
  `inventory.insufficient_stock` (409) y **toda la transacción del pago se revierte**
  (no se sobrevende). Un relajamiento configurable puede llegar más adelante.
- Seeder `SeedSalesAsync`: además de descuentos + gift card, siembra saldo inicial
  (50 u.) por ingrediente y sucursal si `branch_inventory` está vacío, para que el
  descuento por receta tenga stock en un entorno limpio.
- **Sin migración** (solo un método de repositorio, un servicio y cableado de handlers).
- 5 tests unit nuevos (93 total): agrupación por ingrediente, idempotencia del
  consumo, ítems sin receta, reversa opuesta que restaura el saldo, idempotencia de
  la reversa. Verificado e2e vs compose: 2×pizza + 3×empanada → un `SALE` de −0.75
  de harina; split de pago no re-descuenta; cancelar un pedido `PAID` añade el
  `ADJUSTMENT` +0.30 y conserva el `SALE`.
- Con esto la Fase 6 queda **solo pendiente del frontend (6d)**.
