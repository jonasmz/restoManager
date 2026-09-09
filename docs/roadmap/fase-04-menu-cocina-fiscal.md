# Fase 4 — Menú, recetas, cocina y fiscal

## Objetivo y alcance

La oferta comercial y su composición:

- `categories` y `menu_items` (`is_available`, `price`).
- `recipe_items`: qué ingrediente y cuánto consume cada `menu_item`
  (`quantity_required`), con `UNIQUE(menu_item_id, ingredient_id)` (§7.5).
- `kitchen_stations` y `station_menu_items` (qué estación prepara qué plato),
  `UNIQUE(station_id, menu_item_id)`.
- `tax_rates` y `menu_item_taxes` (impuestos por plato),
  `UNIQUE(menu_item_id, tax_rate_id)`.

## Dependencias

Fase 2 (sucursales, para `kitchen_stations`) y Fase 3 (`ingredients`, para
`recipe_items`).

### Decisiones bloqueantes — RESUELTAS (2026-09-09)

1. **Impuestos inclusivos**: el `price` de `menu_items` ya incluye los impuestos
   asignados. En la cuenta (Fase 6) se desglosa hacia atrás:
   `base = price / (1 + Σ rate)`, `impuesto_i = base × rate_i`. El `price` es el
   importe final que paga el cliente.
2. **Catálogo global + disponibilidad por sucursal**: `menu_items` sigue siendo
   global (sin `branch_id`), pero se añade una tabla nueva
   `menu_item_branch_availability (branch_id, menu_item_id, is_available)` con
   `UNIQUE(branch_id, menu_item_id)` para poder ocultar un plato en una sucursal
   concreta. Ausencia de fila ⇒ se hereda `menu_items.is_available` (disponible por
   defecto). Esto es un **desvío del esquema** → migración nueva en Fase 4a.
   El scoping de esa tabla usa el header `X-Branch-Id` (ver `transversales.md`).

## Backend

- Entidades: `Category`, `MenuItem`, `RecipeItem`, `KitchenStation`,
  `StationMenuItem`, `TaxRate`, `MenuItemTax`.
- Invariantes: no duplicar par plato-insumo (`RecipeItem`), plato-estación,
  plato-impuesto; `quantity_required > 0`; `rate` de impuesto válido.
- Casos de uso: CRUD de categorías, platos, recetas (añadir/quitar ingrediente),
  estaciones, asignación de platos a estación, tasas e impuestos por plato.
- Endpoints `/api/v1/categories`, `/menu-items` (+ `/{id}/recipe`, `/{id}/taxes`),
  `/kitchen-stations` (+ `/{id}/menu-items`), `/tax-rates`.
- Consulta de apoyo para Fase 6: `GET /menu-items/{id}/cost` (costo teórico según
  receta y `ingredients.unit_price`).

## Frontend

| Ruta | Contenido | Referencia |
|---|---|---|
| `menu/categories` | CRUD categorías | tabla + form |
| `menu/items` | Lista de platos (precio, disponible, categoría) | `inventory.html` |
| `menu/items/:id` | Ficha: datos + **receta** (ingredientes y cantidades) + impuestos | form + tabla editable |
| `kitchen/stations` | Estaciones por sucursal y sus platos | tabla + selector múltiple |
| `fiscal/tax-rates` | CRUD de tasas | tabla + form |

- Menú lateral: grupo "Menú" y (dentro) "Cocina" y "Fiscal".

## Criterios de aceptación

- [x] No se puede añadir dos veces el mismo ingrediente a una receta, ni el mismo
      impuesto a un plato, ni el mismo plato dos veces a una estación (409). *(4a: invariantes
      de dominio `menu.recipe_duplicate_ingredient` / `menu.tax_duplicate` /
      `menu.station_duplicate_item`).*
- [x] `is_available = false` oculta el plato de la toma de pedido (Fase 6) pero no lo
      borra. *(4a: `is_available` en `menu_items` + override por sucursal en
      `menu_item_branch_availability`; el filtrado en la toma de pedido lo aplica la Fase 6).*
- [x] `GET /menu-items/{id}/cost` devuelve el costo teórico = Σ(`quantity_required` ×
      `ingredients.unit_price`). *(4a).*
- [x] La ficha de plato persiste datos + receta + impuestos en una sola operación
      *(4a: `PUT /menu-items/{id}` con `recipe` y `taxRateIds` en el mismo cuerpo).*

## Pruebas

- Unit: invariantes de unicidad y `quantity_required`.
- Integración: CRUD + edición de receta/impuestos + cálculo de costo.
- Frontend: ficha de plato con receta editable.

## Ramas/PR (cortar en 2)

1. `feat/fase-04a-menu-backend` — categorías, platos, recetas, estaciones, impuestos,
   endpoints. **Hecha.** Hexágono `Menu` + `Tax`: `Category`, `MenuItem` (agregado con
   receta e impuestos, reemplazo en bloque), `RecipeItem`, `KitchenStation` (agregado
   con sus platos), `StationMenuItem`, `TaxRate`, `MenuItemTax`, `MenuItemBranchAvailability`
   (tabla nueva, migración `MenuAndFiscal`). Colecciones hijas con
   `DeleteBehavior.ClientCascade` (FK NO ACTION en BD, EF borra huérfanos al reemplazar).
   Endpoints `/api/v1/{categories, menu-items(+ /{id}/recipe, /{id}/taxes, /{id}/cost,
   /{id}/availability), kitchen-stations(+ /{id}/menu-items), tax-rates}` con policy
   `MenuAccess` (ADMIN/BRANCH_MANAGER). Estaciones y disponibilidad por `X-Branch-Id`.
   Seed dev ampliado (3 categorías, IVA 21 %, 3 ingredientes, Pizza Margarita con receta
   e impuesto, estación "Cocina caliente"). 11 tests unitarios nuevos (38 en total).
2. `feat/fase-04b-menu-frontend` — pantallas.
