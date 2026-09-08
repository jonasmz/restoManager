# Reglas transversales del roadmap

Se leen **antes de cualquier fase**. Complementan (no reemplazan)
[`../backend.md`](../backend.md), [`../frontend.md`](../frontend.md),
[`../auth.md`](../auth.md) y [`../arquitectura.md`](../arquitectura.md).

---

## 1. Branch scoping (multisucursal)

Casi todo el dominio cuelga de `branch_id`. Reglas:

- El **JWT** incluye un claim `branch_id` (sucursal operativa del empleado) y, si el
  usuario opera en varias, `branch_ids`.
- El frontend tiene un **selector de sucursal** en el topbar. La sucursal activa es
  una **señal global** (`BranchContextService`) y se envía en cada request a la
  Business API (header `X-Branch-Id` o query, se fija en Fase 2).
- Todo caso de uso que lee/escribe datos de sucursal **DEBE filtrar por la sucursal
  activa** y **DEBE validar `DOM-06`**: el empleado del token está habilitado para esa
  sucursal. Si no, `403`.
- Entidades sin sucursal (catálogo global): `restaurants`, `roles`, `categories`,
  `menu_items`, `recipe_items`, `ingredients` (catálogo), `discounts`, `tax_rates`,
  `customers`, `suppliers`, `gift_cards`. El resto es por sucursal.

---

## 2. Catálogo de estados — BORRADOR, REQUIERE APROBACIÓN

El esquema deja muchos `status`/`type` como `varchar` libre a propósito (spec §14,
MET-01). Este es el **catálogo propuesto**; cada fase confirma con el usuario el suyo
antes de implementar. **No está aprobado todavía.**

| Columna | Valores propuestos | Transiciones | Notas |
|---|---|---|---|
| `orders.status` | `OPEN`, `CLOSED`, `CANCELLED` | `OPEN→CLOSED`, `OPEN→CANCELLED` | **Venta efectiva (MET-01) = `CLOSED`.** |
| `payments.status` | `PENDING`, `CONFIRMED`, `FAILED`, `REFUNDED` | `PENDING→CONFIRMED`, `PENDING→FAILED`, `CONFIRMED→REFUNDED` | Cuenta para métricas = `CONFIRMED`. |
| `payments.payment_method` | `CASH`, `CARD`, `TRANSFER`, `GIFT_CARD`, `OTHER` | — | `GIFT_CARD` genera `gift_card_transactions`. |
| `reservations.status` | `PENDING`, `CONFIRMED`, `SEATED`, `CANCELLED`, `NO_SHOW` | `PENDING→CONFIRMED→SEATED`; `PENDING/CONFIRMED→CANCELLED`; `CONFIRMED→NO_SHOW` | `RESERVED` (derivado) usa `CONFIRMED` dentro de la ventana (TBL-02). |
| `deliveries.status` | `PENDING`, `ASSIGNED`, `IN_TRANSIT`, `DELIVERED`, `FAILED`, `CANCELLED` | `PENDING→ASSIGNED→IN_TRANSIT→DELIVERED`; `ASSIGNED/IN_TRANSIT→FAILED`; `*→CANCELLED` | `actual_time` se informa al llegar a `DELIVERED`/`FAILED`. |
| `purchase_orders.status` | `DRAFT`, `SENT`, `PARTIALLY_RECEIVED`, `RECEIVED`, `CANCELLED` | `DRAFT→SENT→PARTIALLY_RECEIVED→RECEIVED`; `*→CANCELLED` | Movimiento `PURCHASE` al pasar a `PARTIALLY_RECEIVED`/`RECEIVED` (INV-07). |
| `employee_leaves.status` | `REQUESTED`, `APPROVED`, `REJECTED`, `CANCELLED` | `REQUESTED→APPROVED/REJECTED`; `REQUESTED/APPROVED→CANCELLED` | — |
| `employee_leaves.leave_type` | `VACATION`, `SICK`, `UNPAID`, `OTHER` | — | — |
| `discounts.type` | `PERCENTAGE`, `FIXED_AMOUNT` | — | Define cómo se calcula `order_discounts.applied_amount`. |
| `delivery_drivers.vehicle_type` | `MOTORCYCLE`, `BICYCLE`, `CAR`, `ON_FOOT` | — | — |
| `inventory_movements.reference_type` | `PURCHASE_ORDER`, `ORDER`, `WASTE_LOG`, `MANUAL_ADJUSTMENT` | — | Convención del spec §7.4. |

**Fijos por `CHECK` en la BD (no se tocan):** `tables.operational_status`
(`ACTIVE`/`CLEANING`/`OUT_OF_SERVICE`), `orders.channel`
(`MESA`/`BARRA`/`TAKEAWAY`/`DELIVERY`), `inventory_movements.movement_type`
(`PURCHASE`/`SALE`/`WASTE`/`ADJUSTMENT`).

**Implementación:** enums de aplicación (`Domain`) con `HasConversion<string>()` en EF
Core. **No** se añaden `CHECK` nuevos a la BD para estos catálogos: el esquema los
deja libres deliberadamente y las transiciones las valida `Application`.

---

## 3. Transacciones y unidad de trabajo

- Toda operación que escribe **más de una fila relacionada** ocurre en **una sola
  transacción** (spec §1.1; INV-05 para inventario; pagos; pedidos con ítems).
- La transacción la abre el **handler de `Application`** vía `IUnitOfWork`
  (adaptador EF Core en `Infrastructure`). Los repositorios no hacen `SaveChanges`
  por su cuenta.
- Si cualquier invariante falla a mitad, se revierte todo (§11.4 paso 22).

---

## 4. Estados derivados: nunca se persisten

- **Estado de uso de mesa** (`AVAILABLE`/`RESERVED`/`OCCUPIED`): se calcula con
  `tableDisplayStatus(table, now)` (spec Anexo B). Prohibido escribirlo en
  `tables.operational_status` (TBL-01).
- **`RESERVED`**: derivado de `reservation_time` + `reservations.status` + ventana
  (RSV-03).
- **Métricas** (§8): se calculan por consulta sobre datos transaccionales; no hay
  tablas de agregados (MET-02).

---

## 5. Modelo de errores

- Respuestas de error en formato **`ProblemDetails`** (RFC 7807).
- Un punto único traduce **excepción/resultado de dominio → status HTTP**
  (`ValidationException`→422, `NotFound`→404, `DomainRuleViolation`→409,
  autorización→403).
- El `type`/`title` identifican la regla violada (p. ej. `DOM-03`, `SES-02`).

---

## 6. API: paginación, filtrado y orden

- Listados: `?page=1&pageSize=20&sort=campo,-otroCampo`. `pageSize` con tope (p. ej.
  100). Respuesta: `{ items, page, pageSize, total }`.
- Filtros por query string tipados; fechas en ISO-8601 UTC.
- Rutas versionadas: `/api/v1/...`.

---

## 7. Migraciones y datos semilla

- **Estrategia baseline** (se establece en Fase 1/2): la primera migración de
  `resto_business` reproduce **exactamente** `requirements/restaurant_schema.sql`. Se
  valida con `dotnet ef migrations script` comparando contra ese `.sql` (mismas
  tablas, columnas, tipos, `CHECK`, `UNIQUE`, índice único parcial de sesiones).
- Cada fase añade sus **datos semilla mínimos** (idempotentes) para poder probar de
  extremo a extremo: Fase 2 crea 1 restaurante + 2 sucursales + roles + empleados
  demo; fases siguientes añaden lo suyo.
- La Auth API tiene su propia cadena de migraciones (Identity) sobre `resto_identity`.

---

## 8. Idempotencia de inventario (DOM-07)

- Una **operación de origen** (recepción de compra, confirmación de pedido,
  `waste_log`) genera **exactamente un** conjunto de `inventory_movements`.
- Se guarda siempre `reference_type` + `reference_id` (§7.4) y se comprueba que no
  exista ya un movimiento con esa referencia antes de crear otro.
- Si la operación de origen se cancela, se generan **movimientos de reversa**
  (`ADJUSTMENT` o el opuesto), nunca se borran movimientos.
- El **evento exacto** que dispara el movimiento `SALE` de una venta lo decide la
  Fase 6 (spec §7.5 lo deja abierto).

---

## 9. Dinero, fechas y unidades

- Importes: `decimal` en backend, nunca `float`/`double`. Redondeo definido en Fase 6.
- Fechas: `DateTime`/`DateTimeOffset` en **UTC**; columnas Postgres `timestamptz`;
  Npgsql configurado para UTC. El frontend muestra en la zona del navegador.
- Cantidades de inventario: `decimal(10,2)` con la `unit` de `ingredients`.

---

## 10. Construir pantallas que el template no cubre

`requirements/inapp/` solo trae dashboard, inventario, alta de producto, reportes y
auth. Las vistas nuevas (salón, toma de pedido/POS, delivery, reservas, clientes,
compras) se **componen con los mismos componentes y clases del template**, sin
inventar un estilo nuevo:

| Necesidad | Reutiliza del template |
|---|---|
| Tarjeta de indicador | KPI card de `index.html` + `.icon-shape` (`_icon-shape.scss`) |
| Tabla con acciones y paginación | `table table-hover` + `tfoot` de `inventory.html` |
| Formulario de alta/edición | patrón de `create-product.html` (`needs-validation`) |
| Estado con color | `badge` Bootstrap (Completed/Processing/Pending/Cancelled de `index.html`) |
| Lista con miniatura | `list-group` de `reports.html` |
| Diálogo | modal de Bootstrap 5 (`@popperjs/core` ya incluido) |
| Gráfica | `ng-apexcharts` replicando `assets/js/chart.js` |
| Avatares / grupos | `_avatar.scss` |

Layout: todas las vistas de app van dentro del `LayoutComponent` (shell). Las
pantallas tipo "tablero" (salón, POS) usan la rejilla `row`/`col` de Bootstrap y
`card` como contenedor.

---

## 11. Testing por capa (resumen)

| Capa | Qué se prueba | Infra |
|---|---|---|
| `Domain` | invariantes de entidades/value objects | ninguna |
| `Application` | handlers con puertos en doble de prueba | ninguna |
| `Api` integración | endpoint → EF Core → Postgres real | Testcontainers `postgres:17` o servicio compose de test |
| Frontend | `lint` + tests de componente/servicio + comparación visual con el template | contenedor `frontend` |

Detalle en [`../backend.md`](../backend.md) §Testing y [`../frontend.md`](../frontend.md).
Todo cambio de comportamiento llega con pruebas; sin ellas, el PR no está listo.
