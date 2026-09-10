# Fase 10 — Exportación de reportes a PDF

## Objetivo y alcance

Generar **PDF server-side** de los reportes de la Fase 9, con formato y branding
propios (encabezado, sucursal, rango de fechas, totales), reproducibles y aptos para
adjuntar o archivar. CSV/XLSX ya se resuelven en el cliente (Fase 9b); esta fase
añade solo el PDF.

## Dependencias

Fase 9 (endpoints de reportes y criterios MET-01/02).

## Decisión de diseño

- **REST**, coherente con el resto (ver decisión de Fase 9). No GraphQL.
- Librería: **QuestPDF** (licencia Community, gratis bajo el umbral de facturación;
  API declarativa en C#). Alternativa descartada: render headless con Chrome (infra
  pesada).
- Forma del endpoint: `GET /api/v1/reports/{recurso}?format=pdf` **o**
  `GET /api/v1/reports/{recurso}/pdf` — elegir una y documentarla. Mismos filtros
  `?from=&to=` y `X-Branch-Id` que el JSON. Content-Type `application/pdf`,
  `Content-Disposition: attachment`.
- Un endpoint `GET /api/v1/reports/dashboard/pdf` que arme el informe completo
  (KPIs + tablas), y opcionalmente PDF por tabla individual.
- Policy `ReportsAccess` (ADMIN, BRANCH_MANAGER).

### Decisiones bloqueantes — RESUELTAS (2026-09-10)

1. **Ruta `/pdf` dedicada** por reporte (no `?format=pdf`). Refleja 1:1 los endpoints
   JSON de la Fase 9 con sufijo `/pdf`.
2. **Informe completo del dashboard + PDF por cada tabla**: `dashboard/pdf` (KPIs +
   todas las secciones) y un PDF por reporte individual
   (`sales/summary/pdf`, `sales/pdf?groupBy=`, `products/top/pdf`,
   `payments/by-method/pdf`, `discounts/applied/pdf`, `inventory/low-stock/pdf`,
   `purchasing/cost/pdf`, `tables/turnover/pdf`).
3. **Encabezado completo, es-AR fijo, sin logo** (el esquema no tiene columna).
   Encabezado: nombre del restaurante, sucursal, CUIT/NIF, período y fecha de
   emisión. Pie: `Página X de Y` + restaurante/título. Cifras y fechas en es-AR (`$`).

## Backend

- `RestoManager.Business.Api` (o un proyecto `*.Reports.Pdf`): componer el documento
  con QuestPDF a partir de los mismos `IReportQueries` de la Fase 9 (no duplicar
  agregaciones).
- Servicio `IReportPdfRenderer` con un método por informe; los endpoints devuelven
  `Results.File(bytes, "application/pdf", fileName)`.
- Sin entidades ni migraciones.

## Frontend

- En `Reports` / `Dashboard`: botón "PDF" junto a CSV/XLSX que abre/descarga el
  endpoint (`window.open` o `HttpClient` con `responseType: 'blob'` + descarga).
- Reutiliza el rango de fechas y la sucursal activa ya seleccionados.

## Criterios de aceptación

- [ ] El PDF del dashboard refleja los mismos números que la vista para el mismo
      rango y sucursal.
- [ ] Encabezado con sucursal + rango + fecha de emisión; totales por tabla.
- [ ] Descarga con nombre de archivo y `Content-Disposition: attachment`.
- [ ] Las cifras usan formato `es-AR` / `$`.

## Pruebas

- Unit/integración: el renderer produce un PDF no vacío y con las secciones
  esperadas (se puede aserverar longitud/segmentos de texto extraídos).
- e2e: descargar el PDF del dashboard contra datos semilla.

## Ramas/PR

1. `feat/fase-10-export-pdf` — renderer QuestPDF + endpoints + botón en el frontend. **Hecha (PR #26).**

## Estado — Fase 10 COMPLETA (PR #26)

- Paquete `QuestPDF 2025.7.0` en la Api; `QuestPDF.Settings.License =
  LicenseType.Community` en `Program.cs`.
- Modelo agnóstico `ReportDocument` (Application) + `ReportDocumentBuilder`
  (arma KPIs y secciones desde `IReportQueries`; sin dependencia de QuestPDF).
  `IReportQueries.HeaderAsync` nuevo (restaurante + sucursal + CUIT/NIF).
- Port `IReportRenderer` (Application) → `QuestPdfReportRenderer` (Api): A4,
  encabezado/pie, KPIs en tarjetas, cada sección como tabla.
- `MapReportPdfEndpoints`: 9 rutas `GET /api/v1/reports/**/pdf` bajo `ReportsAccess`,
  mismos filtros `?from=&to=` + `X-Branch-Id`, `Results.File(pdf, "application/pdf",
  "<nombre>-<fecha>.pdf")`.
- Frontend: `ReportsApiService.pdf()` (responseType blob), `ExportButtons` gana botón
  **PDF** opcional (callback), `triggerDownload` exportado. Reportes: botón "Informe
  PDF" (dashboard completo) + botón PDF por tabla. Panel: botón PDF con el rango activo.
- e2e vs compose: los 9 endpoints devuelven PDF válido (curl + navegador 200);
  contenido verificado con `pdftotext`. `dotnet test` 128 verdes; `ng lint`/`ng build`
  limpios.

**ROADMAP COMPLETO** (fases 0–10).
