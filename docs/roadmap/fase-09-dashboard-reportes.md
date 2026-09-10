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

### Decisiones bloqueantes — RESUELTAS (2026-09-10)

1. **Venta efectiva (MET-01)**: `orders.status IN ('PAID','CLOSED')`. Se excluyen
   `OPEN` (aún sin cobrar) y `CANCELLED`. Medios de pago: solo
   `payments.status = 'CONFIRMED'` (excluye `PENDING`/`FAILED`/`REFUNDED`). Coincide
   con lo decidido en Fase 6. Cada endpoint documenta el criterio.
2. **Moneda y locale**: `LOCALE_ID = 'es-AR'` + `DEFAULT_CURRENCY_CODE = 'ARS'` en
   todo el frontend (formato `1.234,56`, símbolo `$`). Se registra `es-AR` con
   `registerLocaleData` y se sustituyen los `| number: '1.2-2'` de importes por
   `| currency`. Nada de `₹`/`en-IN`.
3. **Bajo stock**: **umbral por ingrediente**. Columna nueva
   `ingredients.reorder_point decimal(10,2) NOT NULL DEFAULT 0` (migración
   `IngredientReorderPoint`, primer cambio de esquema desde la baseline). Un insumo
   está "bajo stock" en una sucursal cuando `reorder_point > 0` y
   `branch_inventory.stock_quantity <= ingredients.reorder_point`. El CRUD de
   ingredientes (Fase 3) gana el campo. Un override por sucursal
   (`branch_inventory.reorder_point`) queda para una fase futura si hace falta.

**API: REST** (decidido 2026-09-09). Endpoints `/api/v1/reports/...` con filtros por
query string; se minimizan rutas con `groupBy` (p. ej. `reports/sales?groupBy=`).
Nada de GraphQL.

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

1. `feat/fase-09a-reportes-backend` — endpoints de consulta + criterio MET-01. **Hecha (PR #24).**
2. `feat/fase-09b-dashboard-frontend` — dashboard y reportes conectados. **Hecha (PR #25).**

## Estado — Fase 9 COMPLETA

- **9a (PR #24)**: `IReportQueries` + `ReportQueries` (Infra, GROUP BY en SQL). 9
  endpoints `GET /api/v1/reports/{dashboard, sales/summary, sales?groupBy=…,
  products/top, payments/by-method, discounts/applied, inventory/low-stock,
  purchasing/cost, tables/turnover}`, policy `ReportsAccess` (ADMIN, BRANCH_MANAGER).
  Migración `IngredientReorderPoint` (ADD COLUMN). Seeder con reorder points demo.
- **9b (PR #25)**: `features/reports/` — `ReportsApiService` + `reports.models.ts`,
  `chart-theme.ts` (paleta del template), `ExportButtons` genérico + `core/export/
  table-export.ts` (CSV nativo + XLSX con `exceljs`). `Dashboard` reescrito (4 KPIs,
  barras Ventas vs Compras, dona por canal, listas Más vendidos / Bajo stock /
  Ventas recientes, selector 7/30/90 d). `Reports` reescrito (rango de fechas +
  presets, stat cards, área Ventas por día, 7 tablas con botones CSV/XLSX). Ruta
  `/reports` con `roleGuard(ADMIN, BRANCH_MANAGER)`; el panel `/` degrada con aviso
  si el backend responde 403. **Locale `es-AR` + `ARS`** en `app.config.ts`
  (`registerLocaleData`, `LOCALE_ID`, `DEFAULT_CURRENCY_CODE`); todos los importes de
  la app pasaron de `| number: '1.2-2'` a `| currency`.
- `ng lint` + `ng build` limpios; verificado en navegador (dashboard, reportes,
  export CSV/XLSX sin errores de consola).

## Sigue: Fase 10 (PDF)

Exportación a **PDF server-side** con **QuestPDF** y plantilla propia. Endpoint(s)
`/api/v1/reports/**/pdf` o `?format=pdf`. Fase chica; ver `docs/roadmap/
fase-10-export-pdf.md`.
