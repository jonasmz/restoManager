# Roadmap de implementación

Este documento y la carpeta [`roadmap/`](roadmap/) describen **cómo se construye el
sistema, fase a fase**, para que un agente pueda tomar una fase por sesión y
ejecutarla sin ambigüedad.

## Cómo se usa

1. Elige la **primera fase con estado "Pendiente"** cuyas dependencias estén "Hecho".
2. Lee, en este orden:
   - [`roadmap/transversales.md`](roadmap/transversales.md) — reglas que aplican a
     todas las fases (branch scoping, catálogos de estado, transacciones, errores…).
   - `roadmap/fase-XX-*.md` de la fase elegida.
   - El `docs/*.md` de la capa que vas a tocar
     ([`backend.md`](backend.md), [`auth.md`](auth.md), [`frontend.md`](frontend.md),
     [`docker.md`](docker.md)) y [`flujo-de-trabajo.md`](flujo-de-trabajo.md).
   - Las secciones de `requirements/restaurant_schema_specifications.md` que la fase
     referencia.
3. **Resuelve con el usuario las "decisiones bloqueantes" de la fase antes de
   implementar** (cada `fase-XX` las lista al principio).
4. Sigue el flujo de [`git-github.md`](git-github.md): **una fase = una o más
   ramas/PR**. Las fases grandes (2, 3, 6) se parten en varios PR; el `fase-XX`
   indica el corte sugerido.
5. Al terminar, marca la fase como "Hecho" en la tabla de abajo (en su propio PR o en
   el de la fase).

## Estado de las fases

| Fase | Objetivo | Entidades del esquema | Spec | Dependencias | Estado |
|---|---|---|---|---|---|
| [0 — Andamiaje](roadmap/fase-00-andamiaje.md) | Infra Docker + soluciones .NET hexagonales vacías + shell Angular del template | — | — | — | En curso (PR #2) |
| [1 — Auth y usuarios](roadmap/fase-01-auth.md) | Auth API (Identity, JWT asimétrico, JWKS) + validación en Business API + login en el frontend | *(ASP.NET Identity, base `resto_identity`)* | [`auth.md`](auth.md) | 0 | Implementada (PR #3 · #4 · #5, apilados; pendiente de merge) |
| [2 — Organización](roadmap/fase-02-organizacion.md) | Empresa, sucursales y personal; **branch scoping** transversal; selector de sucursal; datos semilla | `restaurants`, `branches`, `departments`, `roles`, `employees`, `shifts`, `employee_leaves` | §2, DOM-06 | 1 | **Hecha** (PR #7, #8, #9). Pendiente futuro: seed real parametrizable y vínculo `AppUser`↔`employees` |
| [3 — Inventario y compras](roadmap/fase-03-inventario-compras.md) | Saldo por sucursal + ledger firmado atómico; proveedores y recepción de compra; mermas | `ingredients`, `branch_inventory`, `inventory_movements`, `waste_logs`, `suppliers`, `purchase_orders`, `purchase_order_items` | §7, §11.4, §11.6, INV-01…08, DOM-05/07 | 2 | **Hecha** (PR #7 backend, #10 frontend). Branch scoping alineado al header `X-Branch-Id` |
| [4 — Menú, recetas, cocina y fiscal](roadmap/fase-04-menu-cocina-fiscal.md) | Carta, recetas (`menu_item` ↔ `ingredient`), estaciones de cocina, impuestos | `categories`, `menu_items`, `recipe_items`, `kitchen_stations`, `station_menu_items`, `tax_rates`, `menu_item_taxes` (+ `menu_item_branch_availability`) | §7.5, §2 | 2, 3 | **Hecha** (PR #11 backend, #12 frontend). Impuestos **inclusivos**; carta global + disponibilidad por sucursal |
| [5 — Salón](roadmap/fase-05-salon.md) | Mesas + sesiones (una abierta por mesa) + estado derivado (Anexo B) + reservas | `tables`, `table_sessions`, `reservations` | §4, §6, §11.1/11.3, SES-*, TBL-*, RSV-*, DOM-03/04 | 2 | **Hecha** (PR #13 backend, #14 frontend). Ventana `RESERVED` 30/30 min configurable; alta rápida de cliente para reservas (Fase 8 se hace dueña) |
| [6 — Ventas](roadmap/fase-06-ventas.md) | Pedidos con canal explícito, ítems, impuestos, descuentos, pagos múltiples y **descuento de stock por receta** (idempotente + reversa) | `orders`, `order_items`, `payments`, `discounts`, `order_discounts`, `gift_card_transactions` | §5, §7.5, §11.1/2/5/7, ORD-*, DOM-01/02/07, MET-01 | 4, 5 | **Hecha** (PR #16 pedidos, #17 descuentos/pagos, #18 consumo de stock, #19 POS). `orders.status` OPEN→PAID→CLOSED; SALE al 1.er pago CONFIRMED; redondeo medio-arriba |
| [7 — Delivery](roadmap/fase-07-delivery.md) | Canal `DELIVERY` ⇒ fila en `deliveries` (DOM-08); repartidores; seguimiento | `delivery_drivers`, `deliveries` | §5.4, DOM-08 | 6 | **Hecha** (PR #20 backend, #21 frontend). Entrega creada en la misma tx que el pedido DELIVERY; cancelar entrega cancela el pedido |
| [8 — Clientes y fidelización](roadmap/fase-08-clientes-fidelizacion.md) | Clientes, puntos de fidelidad, gift cards (emisión/canje), reseñas | `customers`, `reviews`, `gift_cards`, `gift_card_transactions` | §3, CUS-*, §3.3 | 6 | **Hecha** (PR #22 backend, #23 frontend). Fidelidad: 1 pt/unidad al cerrar, canje 100 pts = 1 unidad (`Loyalty:RedeemRate`); gift cards con `customer_id` obligatorio y sin recarga; sin entidades anónimas. POS: selector «Identificar cliente» + canje de puntos desde el pedido abierto |
| [9 — Dashboard y reportes](roadmap/fase-09-dashboard-reportes.md) | Métricas derivadas de datos transaccionales; dashboard y reportes del template con datos reales | *(solo lectura + `ingredients.reorder_point`)* | §8, §12, MET-01/02 | 6, 7, 8 | **Hecha** (PR #24 backend, #25 frontend). REST `/api/v1/reports/*`; venta efectiva = PAID+CLOSED; pagos CONFIRMED; bajo stock por `ingredients.reorder_point`. Frontend: dashboard + reportes con `ng-apexcharts`, locale `es-AR`/`$`, export CSV/XLSX por tabla |
| [10 — Exportación de reportes a PDF](roadmap/fase-10-export-pdf.md) | Endpoint(s) de PDF server-side con plantilla propia (QuestPDF) para reportes branded/reproducibles | *(solo lectura)* | §8 | 9 | **Hecha** (PR #26). 9 rutas `/api/v1/reports/**/pdf` (QuestPDF, encabezado con restaurante/sucursal/CUIT/período, es-AR); botones PDF en panel y reportes |
| [11 — Carta pública / QR](roadmap/fase-11-carta-publica.md) | Carta pública por sucursal accesible por QR: filtro por categoría, buscador, cards con imagen/ingredientes/precio y carrito de estimación | *(desvío: `menu_items.image_key`, `branches.public_slug` + almacén de imágenes)* | — (alcance nuevo) | 2, 4 | **Hecha** (PR #30 backend, 11b frontend). Endpoint anónimo `/api/v1/public/catalog/{slug}`; imágenes servidas en `/media/menu`; disponibilidad por sucursal respetada; carrito de estimación en el cliente; slug + QR en la ficha de sucursal |
| [12 — Combos](roadmap/fase-12-combos.md) | Combos de platos (± bebida) con descuento plano: % sobre la suma de sus platos o precio fijo. ABM en el menú, uso en el POS (se expande en líneas + ahorro congelado) y en la carta pública | *(desvío: `combos`, `combo_items`, `order_combos` + `order_items.order_combo_id`)* | — (alcance nuevo) | 4, 6, 11 | **Pendiente** — spec lista (2026-09-10). Sin condición de medio de pago; disponibilidad por sucursal derivada de los componentes; el ahorro cuenta como descuento en reportes |

## Grafo de dependencias

```mermaid
flowchart TD
    F0[0 · Andamiaje] --> F1[1 · Auth]
    F1 --> F2[2 · Organización]
    F2 --> F3[3 · Inventario y compras]
    F2 --> F5[5 · Salón]
    F3 --> F4[4 · Menú, cocina, fiscal]
    F2 --> F4
    F4 --> F6[6 · Ventas]
    F5 --> F6
    F6 --> F7[7 · Delivery]
    F6 --> F8[8 · Clientes y fidelización]
    F6 --> F9[9 · Dashboard y reportes]
    F7 --> F9
    F8 --> F9
    F2 --> F11[11 · Carta pública / QR]
    F4 --> F11
    F4 --> F12[12 · Combos]
    F6 --> F12
    F11 --> F12
```

## Cobertura del esquema

Las 38 tablas de `requirements/restaurant_schema.sql` se reparten así (cada tabla en
**una sola** fase "dueña"; otras fases la consumen):

- **Fase 2:** restaurants, branches, departments, roles, employees, shifts, employee_leaves
- **Fase 3:** ingredients, branch_inventory, inventory_movements, waste_logs, suppliers, purchase_orders, purchase_order_items
- **Fase 4:** categories, menu_items, recipe_items, kitchen_stations, station_menu_items, tax_rates, menu_item_taxes
- **Fase 5:** tables, table_sessions, reservations
- **Fase 6:** orders, order_items, payments, discounts, order_discounts, gift_card_transactions
- **Fase 7:** delivery_drivers, deliveries
- **Fase 8:** customers, reviews, gift_cards, gift_card_transactions *(gift_card_transactions se crea en Fase 6 como medio de pago; Fase 8 añade emisión y saldo de la tarjeta)*

El checklist del spec (§15) queda cubierto por los **criterios de aceptación**
repartidos entre las fases 2–9.

Las fases **posteriores al roadmap original** añaden tablas/columnas fuera de las 38 del
esquema base (desviaciones documentadas en cada `fase-XX` con su migración EF):

- **Fase 11:** `menu_items.image_key`, `branches.public_slug` (+ índice único).
- **Fase 12:** `combos`, `combo_items`, `order_combos` (+ `order_items.order_combo_id`).
