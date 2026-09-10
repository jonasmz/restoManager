# Fase 8 — Clientes y fidelización

## Objetivo y alcance

Identidad persistente del cliente y beneficios:

- `customers` (nombre, contacto, `loyalty_points`) — identidad de negocio, **no** un
  asiento ni un consumidor temporal (CUS-01). La app **nunca** crea clientes técnicos
  (CUS-03) y las ventas anónimas siguen con `orders.customer_id = NULL` (CUS-02).
- `reviews` (rating 1–5 por `CHECK`, `comment` opcional).
- `gift_cards` (emisión, saldo, `expiry_date`, `card_number` `UNIQUE`) y
  `gift_card_transactions` (canje contra pedidos — el medio de pago se implementó en
  Fase 6; aquí se añade emisión, recarga y control de saldo).

## Dependencias

Fase 6 (pedidos y `gift_card_transactions` como medio de pago).

### Decisiones bloqueantes — RESUELTAS (2026-09-09)

1. **Política de fidelidad**:
   - **Acumulación**: `1 punto por unidad monetaria` → al cerrar el pedido
     (`CloseOrder`), si `orders.customer_id` no es NULL, se suman
     `floor(orders.total_amount)` puntos al cliente, en la misma transacción. Solo
     una vez (el cierre es terminal). Ventas anónimas no acumulan.
   - **Canje**: `RedeemLoyaltyPoints(orderId, points)` sobre un pedido `OPEN` con
     `customer_id`. Convierte puntos en un `OrderDiscount` de importe
     `round(points / RedeemRate)` (ratio configurable `Loyalty:RedeemRate`,
     por defecto `100` → 100 pts = 1 unidad monetaria). Resta los puntos al cliente
     en la misma transacción. Un solo canje por pedido (`UNIQUE(order_id,
     discount_id)`); se rechaza si el importe supera el total del pedido
     (`loyalty.redemption_exceeds_total`) o si el cliente no tiene puntos suficientes
     (`loyalty.insufficient_points`). El canje se apoya en una fila de `discounts` de
     sistema («Canje de puntos de fidelidad», `FIXED_AMOUNT`) que siembra el seeder
     de forma idempotente; el importe real se congela en `order_discounts.applied_amount`.
2. **Gift cards**: la emisión (`IssueGiftCard`) **exige `customer_id`** (como el
   esquema, §3.3). **Sin recarga**: el saldo solo baja por canje (Fase 6). Tarjeta
   con `expiry_date < hoy` → `409 sales.gift_card_expired` al canjear (validación
   añadida en `GiftCard.Redeem`), pero el saldo sigue siendo consultable.
   `card_number` duplicado → `409 gift_cards.duplicate_card_number`.
3. **Anónimos**: **NO**. Se mantiene `customer_id NOT NULL` en `reviews`,
   `gift_cards` y `reservations`; sin cambio de esquema. Solo `orders.customer_id`
   admite NULL (venta anónima, CUS-02).

## Backend

- Entidades: `Customer`, `Review`, `GiftCard`, `GiftCardTransaction` (ya existe;
  ampliar).
- Reglas:
  - `Review.rating` ∈ 1..5 (respaldo del `CHECK`); `Review.branch_id` válido.
  - `GiftCard.card_number` único (respaldo del `UNIQUE`); `balance >= 0`; no canjear
    tarjeta expirada.
  - `GiftCardTransaction` siempre ligada a un `order_id`; la suma de transacciones no
    deja el saldo negativo; se escribe en la misma transacción que el `Payment`
    `GIFT_CARD` de Fase 6.
  - Acumulación/canje de puntos según decisión 1, disparada por `CloseOrder` (Fase 6).
- Casos de uso: CRUD `customers`; `IssueGiftCard`, `TopUpGiftCard`, `GetGiftCardBalance`;
  `CreateReview`; `EarnLoyaltyPoints` / `RedeemLoyaltyPoints`.
- Endpoints `/api/v1/customers` (+ `/{id}/orders`, `/{id}/gift-cards`,
  `/{id}/loyalty`), `/gift-cards` (+ `/{cardNumber}/balance`), `/reviews`.
- Integración con Fase 6: `RegisterPayment(method=GIFT_CARD)` valida saldo aquí.

## Frontend

| Ruta | Contenido | Referencia |
|---|---|---|
| `customers` | Lista + buscador de clientes | `inventory.html` |
| `customers/:id` | Ficha: datos, historial de pedidos, puntos, gift cards | `list-group` + tabs |
| `customers/:id/gift-cards` | Emitir / recargar / consultar saldo | form + tabla |
| `reviews` | Listado de reseñas por sucursal, con rating (estrellas) | `list-group` + `badge` |

- En el **POS** (Fase 6): botón "Identificar cliente" que asocia `customer_id` al
  pedido (§5.3) sin implicar exclusividad de la mesa.
- Menú lateral: grupo "Clientes".

## Criterios de aceptación

- [ ] Registrar una venta presencial no crea ningún `customer` (CUS-02/03, §15).
- [ ] `reviews.rating` fuera de 1–5 se rechaza en la app (además del `CHECK`).
- [ ] `gift_cards.card_number` duplicado se rechaza (409); no se canjea tarjeta
      expirada ni por más de su saldo.
- [ ] Pagar con gift card (Fase 6) descuenta saldo y crea `GiftCardTransaction` en la
      misma transacción que el `Payment`.
- [ ] Cerrar un pedido acumula puntos según la política aprobada; el canje los resta.
- [ ] La ficha de cliente muestra historial de pedidos (`/customers/{id}/orders`).

## Pruebas

- Unit: `Review` (rating), `GiftCard` (saldo, expiración, unicidad), cálculo de
  puntos.
- Integración: emisión + canje de gift card contra un pedido real; acumulación de
  puntos al cerrar; rechazo de tarjeta expirada.
- Frontend: ficha de cliente, gift cards, identificar cliente desde el POS.

## Ramas/PR (cortar en 2)

1. `feat/fase-08a-clientes-backend` — clientes, reseñas, gift cards, puntos, endpoints.
2. `feat/fase-08b-clientes-frontend` — pantallas + integración con el POS.
