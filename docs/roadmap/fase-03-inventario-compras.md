# Fase 3 — Inventario y compras

## Objetivo y alcance

Trazabilidad de materia prima por sucursal:

- Catálogo de `ingredients` (sin stock global, INV-01).
- Saldo por sucursal en `branch_inventory` (INV-02, INV-03).
- Ledger firmado `inventory_movements` (INV-04, INV-06) actualizado **atómicamente**
  con el saldo (INV-05).
- Proveedores, órdenes de compra y **recepción** que genera movimientos `PURCHASE`
  (INV-07, flujo §11.4).
- Registro de mermas `waste_logs` que genera movimientos `WASTE` (INV-08, flujo
  §11.6).
- Ajustes manuales (`ADJUSTMENT`).

## Dependencias

Fase 2 (sucursales, empleados, `DOM-06`).

### Decisiones (resueltas 2026-09-09)

1. **Solo recepción total.** Recibir una OC postea `PURCHASE` por la `quantity`
   completa de cada ítem y pasa la OC a `RECEIVED`. Sin `received_quantity` ni
   recepciones parciales (el esquema no lo soporta).
2. **`waste_log` se confirma al crearse.** El log y el movimiento `WASTE` se
   escriben en la misma transacción; sin paso de aprobación.
3. **Carga inicial = movimiento `ADJUSTMENT`** con `reference_type='INITIAL_LOAD'`
   (mantiene el ledger completo). Se rechaza si ya hay saldo > 0.
4. **`purchase_orders.status` aprobado**: `DRAFT` → `SENT` → (`PARTIALLY_RECEIVED`) →
   `RECEIVED`; `* → CANCELLED`.

### Estado

**✅ Fase completa.**

- **Backend (PR #7).** Migración baseline `InitialSchema` (las 35 tablas de
  `restaurant_schema.sql`, entregable de la Fase 2a) + dominio/aplicación/API de
  inventario y compras. Verificado de extremo a extremo contra el compose (carga
  inicial, recepción de OC, merma, movimientos §12.3, saldo no negativo,
  idempotencia, policy `InventoryAccess`).
- **Frontend + alineación de branch scoping (PR #10).** Pantallas de existencias,
  ingredientes, movimientos, proveedores y órdenes de compra. En el mismo PR se
  **eliminó el parámetro `branchId`** de los comandos/consultas/contratos/endpoints
  de Fase 3: la sucursal se resuelve siempre por `IBranchContext` (header
  `X-Branch-Id`, mecanismo introducido en Fase 2), de modo que el interceptor del
  frontend basta y no hay plomería de `branchId` en la UI. `dotnet build`/`test`
  verdes; e2e por navegador (login, carga inicial, merma, crear/enviar/recibir OC
  con descuento de stock, cambio de sucursal) sin errores de consola.

## Backend

- Entidades: `Ingredient` (catálogo), `BranchInventory` (saldo, `>= 0`),
  `InventoryMovement` (tipo + signo, `quantity != 0`, referencia polimórfica),
  `WasteLog`, `Supplier`, `PurchaseOrder` + `PurchaseOrderItem`.
- **Servicio central de inventario** (`Application`):
  `IInventoryLedger.Post(branchId, ingredientId, type, signedQty, reference, employeeId)`
  que, en **una transacción**:
  1. valida `DOM-05` (misma sucursal/ingrediente que el saldo),
  2. valida idempotencia por `reference_type`+`reference_id` (DOM-07),
  3. inserta el `InventoryMovement`,
  4. actualiza (`upsert`) `BranchInventory`,
  5. rechaza si el saldo quedaría `< 0` (salvo `ADJUSTMENT` con política definida).
- Casos de uso:
  - `ReceivePurchaseOrder` — marca la recepción (total o parcial según decisión 1) y
    postea `PURCHASE` por cada ítem recibido.
  - `RegisterWaste` — crea `WasteLog` y postea `WASTE`.
  - `AdjustStock` — postea `ADJUSTMENT` con motivo/auditoría.
  - CRUD de `ingredients`, `suppliers`, `purchase_orders` (borrador → envío).
- Endpoints `/api/v1/ingredients`, `/suppliers`, `/purchase-orders`
  (+ `/{id}/receive`), `/inventory` (saldo por sucursal), `/inventory/movements`
  (historial, §12.3), `/waste-logs`, `/inventory/adjustments`.

## Frontend

| Ruta | Contenido | Referencia |
|---|---|---|
| `inventory` | Saldo por sucursal: tabla (ingrediente, unidad, stock, valor), buscador, filtros | `inventory.html` (es su mapeo directo) |
| `inventory/ingredients/new` | Alta de ingrediente | `create-product.html` |
| `inventory/movements` | Historial de movimientos de un ingrediente | tabla + filtros de fecha/tipo |
| `inventory/waste` | Registrar y listar mermas | form + tabla |
| `purchasing/suppliers` | CRUD proveedores | tabla + form |
| `purchasing/orders` | Órdenes de compra + recepción | tabla + detalle + modal de recepción |

- Menú lateral: grupos "Inventario" y "Compras".

## Criterios de aceptación

- [ ] `ingredients` nunca expone stock; el stock siempre viene de `branch_inventory`
      (INV-01).
- [ ] Todo cambio de `stock_quantity` tiene su `inventory_movement` (INV-04) y ambos
      se escriben en la misma transacción (INV-05); si algo falla, no queda nada.
- [ ] Recibir una orden de compra postea `PURCHASE` positivo y sube el saldo; crear la
      orden **no** mueve stock (INV-07).
- [ ] Confirmar una merma postea `WASTE` negativo por la misma cantidad (INV-08).
- [ ] Reintentar la misma recepción/merma **no** duplica movimientos (DOM-07).
- [ ] El saldo nunca queda negativo (INV-03); el intento devuelve `409`.
- [ ] Historial de movimientos ordenado por `movement_time, id` (§12.3).

## Pruebas

- Unit: `InventoryLedger` (signos, idempotencia, no-negativo, atomicidad simulada).
- Integración: recepción de compra completa/parcial, merma, ajuste, y el rollback
  ante fallo a mitad de transacción.
- Frontend: pantallas de inventario, recepción y mermas.

## Ramas/PR

1. `feat/fase-03` — backend completo (03a core + 03b compras) + migración baseline.
   Entregado en **PR #7**.
2. `feat/fase-03c-inventario-frontend` — pantallas + alineación del branch scoping
   al header `X-Branch-Id`. Entregado en **PR #10**.

### Rutas del frontend (implementadas)

| Ruta | Componente | Contenido |
|---|---|---|
| `inventory` | `StockPage` | Saldo de la sucursal activa; buscador; acciones Carga inicial / Ajuste / Merma en panel embebido |
| `inventory/ingredients` | `IngredientsPage` | CRUD del catálogo (sin stock) |
| `inventory/movements` | `MovementsPage` | Historial §12.3 con filtros ingrediente/tipo/fecha |
| `purchasing/suppliers` | `SuppliersPage` | CRUD de proveedores |
| `purchasing/orders` | `PurchaseOrdersPage` | Lista + alta con líneas + acciones enviar/recibir/cancelar + detalle de ítems |

Menú lateral: grupos «Inventario» y «Compras», visibles para
`ADMIN`/`BRANCH_MANAGER`/`INVENTORY`.
