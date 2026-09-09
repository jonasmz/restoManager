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

### Decisiones bloqueantes

1. ¿Los **impuestos** son aditivos (se suman al `price`) o inclusivos (ya dentro del
   `price`)? — condiciona el cálculo de la cuenta en Fase 6.
2. ¿`menu_items` es catálogo **global** o puede variar por sucursal? (el esquema no
   tiene `branch_id` en `menu_items` → global; confirmar disponibilidad por sucursal
   si se necesita).

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

- [ ] No se puede añadir dos veces el mismo ingrediente a una receta, ni el mismo
      impuesto a un plato, ni el mismo plato dos veces a una estación (409).
- [ ] `is_available = false` oculta el plato de la toma de pedido (Fase 6) pero no lo
      borra.
- [ ] `GET /menu-items/{id}/cost` devuelve el costo teórico = Σ(`quantity_required` ×
      `ingredients.unit_price`).
- [ ] La ficha de plato permite editar la receta y los impuestos y persiste en una
      sola operación.

## Pruebas

- Unit: invariantes de unicidad y `quantity_required`.
- Integración: CRUD + edición de receta/impuestos + cálculo de costo.
- Frontend: ficha de plato con receta editable.

## Ramas/PR (cortar en 2)

1. `feat/fase-04a-menu-backend` — categorías, platos, recetas, estaciones, impuestos,
   endpoints.
2. `feat/fase-04b-menu-frontend` — pantallas.
