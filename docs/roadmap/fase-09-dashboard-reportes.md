# Fase 9 — Dashboard y reportes

## Objetivo y alcance

Convertir los datos transaccionales en información. **Solo lectura**; no hay tablas de
agregados (§8, MET-02): todo se calcula por consulta.

- **Dashboard** (`requirements/inapp/src/index.html`): KPIs reales, 3 gráficas
  ApexCharts con datos reales, listas Top/Low stock/Recent.
- **Reportes** (`requirements/inapp/src/reports.html`): analítica con filtros de
  período, sucursal, canal, empleado, categoría.
- Todas las métricas de la tabla del spec §8 y las consultas conceptuales §12.

## Dependencias

Fases 6 (ventas), 7 (delivery) y 8 (clientes) — para tener datos que reportar. El
dashboard "con datos mock" ya existe desde Fase 0; aquí se conecta a datos reales.

### Decisiones bloqueantes

1. Confirmación final de qué `orders.status` cuentan como **venta efectiva** (MET-01)
   y de qué `payments.status` cuentan para medios de pago.
2. Moneda y locale de presentación (el template trae `₹`/`en-IN` heredado —
   reemplazar).
3. Definición de "producto con bajo stock" (umbral por ingrediente/plato).

## Backend

- Módulo de consultas de solo lectura (`Application` con lecturas directas / vistas
  SQL; **no** entidades nuevas). Cada endpoint acepta `?from=&to=&branchId=` y otros
  filtros según el caso.
- Endpoints `/api/v1/reports/...`:
  - `sales/summary` — ventas por período, nº de pedidos, ticket promedio
    (`AVG(total_amount)` sobre pedidos contabilizados).
  - `sales/by-channel` (§12.5), `sales/by-branch`, `sales/by-employee`,
    `sales/by-category`.
  - `products/top` — unidades y monto por `menu_item` (`order_items` + `menu_items`).
  - `payments/by-method`.
  - `discounts/applied` (`order_discounts` + `discounts`).
  - `inventory/stock` (§12.2), `inventory/movements` (§12.3),
    `inventory/low-stock`.
  - `purchasing/cost` — costo de compras (`purchase_order_items`).
  - `tables/turnover` — rotación/ocupación (`table_sessions.opened_at/closed_at/
    guest_count`).
  - `dashboard` — un único payload con lo que necesita `index.html`.
- Todas las consultas **filtran por los `status` de venta efectiva** (MET-01) y
  documentan el criterio.

## Frontend

| Ruta | Contenido | Referencia |
|---|---|---|
| `` (dashboard) | KPIs (`icon-shape` cards), "Sales vs Purchase" (barras), "Customers Overview" (radialBar), listas Top Selling / Low Stock / Recent Sales | `index.html` (1:1) |
| `reports` | Stat cards + "Sales Overview" (área) con selector de rango + "Top Products" (`list-group`) + tablas por canal/sucursal/empleado/categoría | `reports.html` |

- Gráficas con `ng-apexcharts` replicando `assets/js/chart.js` (mismos tipos y
  colores: `#E66239`, `#f7a085`, `#5BE49B`, `#198754`), pero con **datos del backend**
  y **moneda/locale del proyecto** (decisión 2).
- Filtros de período y sucursal (usa el `BranchContextService`).

## Criterios de aceptación

- [ ] Ninguna métrica se lee de una tabla de agregados; todo deriva de
      `orders`/`order_items`/`payments`/`inventory_movements`/`table_sessions`
      (MET-02, §8).
- [ ] Las consultas de ventas excluyen borradores y cancelaciones (MET-01, §15).
- [ ] El dashboard reproduce `index.html` (layout, colores, gráficas) con cifras
      reales de la sucursal activa y el rango elegido.
- [ ] "Ventas por canal" coincide con `SELECT channel, COUNT(*), SUM(total_amount)
      GROUP BY channel` (§12.5) filtrado por estado efectivo.
- [ ] Rotación de mesas calculada desde `table_sessions` (duración media, comensales,
      nº de sesiones por mesa).
- [ ] Sin `₹`/`en-IN`: moneda y formato del proyecto.

## Pruebas

- Integración: cada endpoint de reporte contra un set de datos semilla conocido
  (aserciones sobre totales, promedios, agrupaciones).
- Frontend: dashboard y reportes con datos de prueba; comparación visual con el
  template.

## Ramas/PR (cortar en 2)

1. `feat/fase-09a-reportes-backend` — endpoints de consulta + criterio MET-01.
2. `feat/fase-09b-dashboard-frontend` — dashboard y reportes conectados.
