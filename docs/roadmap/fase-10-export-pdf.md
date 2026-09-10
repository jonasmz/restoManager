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

### Decisiones bloqueantes

1. `?format=pdf` en el endpoint existente vs. ruta `/pdf` dedicada.
2. ¿Solo el informe completo del dashboard, o también un PDF por cada tabla?
3. Encabezado/pie: ¿logo del restaurante (de `restaurants`), datos fiscales,
   paginación? ¿Idioma fijo `es-AR`?

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

1. `feat/fase-10-export-pdf` — renderer QuestPDF + endpoints + botón en el frontend.
