# Fase 11 — Carta pública / QR

## Objetivo y alcance

Una **carta pública por sucursal**, accesible sin autenticación a través de un código
QR. Muestra la oferta comercial (platos por categoría, con imagen, descripción,
ingredientes y precio), permite **filtrar por categoría** y **buscar**, y ofrece un
**carrito** de solo estimación: el cliente arma su pedido y consulta el total
estimado. **No** crea pedidos ni toca datos transaccionales.

Alcance nuevo, posterior al roadmap 0–10.

## Dependencias

- Fase 2 (sucursales) y Fase 4 (carta: categorías, platos, recetas,
  `menu_item_branch_availability`).

## Decisiones bloqueantes — RESUELTAS (2026-09-10)

1. **Imágenes de plato:** subida de archivo + almacenamiento propio (no solo URL).
   Puerto `IImageStorage`; adaptador sobre sistema de archivos (`LocalImageStorage`,
   volumen `menu_images`). La API sirve las imágenes en `/media/menu/<clave>`, fuera
   del muro de autenticación. Formatos JPG/PNG/WebP, máx. 2 MB (validado en la capa
   de aplicación). Alternativa descartada: columna `bytea` / tabla de blobs.
2. **Alcance por sucursal:** la URL pública lleva el **slug de la sucursal**
   (`branches.public_slug`, único, `[a-z0-9-]` 3–60). La carta respeta la
   disponibilidad por sucursal (`menu_item_branch_availability`; el override gana
   sobre `menu_items.is_available`). La sucursal se resuelve por slug, nunca por
   `X-Branch-Id` ni por claims.
3. **QR:** se genera y se muestra/descarga desde el admin (página de la sucursal).
   Frontend, Fase 11b.
4. **Empaquetado:** dos PR — `feat/fase-11a-*` (backend) y `feat/fase-11b-*`
   (frontend).

## Desviaciones del esquema

Como en Fase 4 con `menu_item_branch_availability`, esta fase añade columnas fuera de
`requirements/restaurant_schema.sql` (migración `Fase11CartaPublica`):

- `menu_items.image_key varchar(200) NULL` — clave de la imagen ilustrativa.
- `branches.public_slug varchar(60) NULL` + índice único — identificador público de
  la carta.
- Almacenamiento de archivos de imagen en un volumen (`menu_images`), servido como
  estáticos por la Business API.

## Backend (Fase 11a)

- **Puerto** `IImageStorage` (`Application/Abstractions`) + adaptador
  `LocalImageStorage` (`Infrastructure/Storage`). Ruta configurable en
  `Storage:MenuImagesPath` (resuelta a absoluta en `Program.cs` y compartida con la
  infraestructura). `Program.cs`: `UseStaticFiles` sobre esa carpeta en `/media/menu`,
  **fuera** de `if (authEnabled)`.
- **Dominio:** `MenuItem.ImageKey` + `SetImage`/`ClearImage`;
  `Branch.PublicSlug` + `SetPublicSlug` (normaliza a minúsculas, valida formato;
  regla `branch.invalid_slug`, 409). Nueva excepción `InvalidInputException` (400)
  para archivos mal formados.
- **Endpoints admin** (grupos autenticados existentes):
  - `PUT /api/v1/menu-items/{id}/image` — `multipart/form-data`, campo `file`.
    `SetMenuItemImageHandler`: valida tipo/tamaño, sube, borra la imagen anterior;
    devuelve `{ imageUrl }`. Reglas `menu.image_type` / `menu.image_size` (400).
  - `DELETE /api/v1/menu-items/{id}/image` — `ClearMenuItemImageHandler`.
  - `PUT /api/v1/branches/{id}/public-slug` (grupo `OrgAdmin`) — body `{ slug }`
    (`null`/vacío lo quita). `409 branch.slug_taken` si otra sucursal ya lo usa.
  - `MenuItemDto` y `BranchDto` ganan `imageUrl` / `publicSlug`.
- **Endpoint público anónimo** (`PublicCatalogEndpoints`, grupo `/api/v1/public`, sin
  `RequireAuthorization`, mapeado siempre):
  - `GET /api/v1/public/catalog/{slug}` → `PublicCatalogDto`
    (`restaurant`, `branch`, `categories`, `items`). `items` solo con platos
    disponibles en la sucursal; `ingredients` = nombres ordenados, sin unidades ni
    cantidades; `categories` = solo las que tienen algún plato visible.
    `404 catalog.not_found` si el slug no existe.
  - `GetPublicCatalogHandler` (Application) sobre repositorios ya existentes +
    `IBranchRepository.GetByPublicSlugAsync`. Sin GraphQL (coherente con Fase 9/10).
- **Semilla dev:** `SeedBranchPublicSlugsAsync` asigna `centro` / `norte` a las
  sucursales demo.
- **CORS:** el origen del frontend debe estar en `Cors:Origins` (ya lo está en dev,
  `http://localhost:4200`).

### Nota sobre pruebas de integración

`GetPublicCatalogHandler` se prueba con fakes de repositorio (patrón del resto de la
capa de aplicación). No hay arnés de integración con Testcontainers en el proyecto
todavía (igual que `ReportQueries` de la Fase 9): las pruebas de extremo a extremo del
endpoint quedan pendientes de ese arnés compartido.

## Frontend (Fase 11b)

Ruta pública `carta/:slug` **fuera** del shell (`Layout`) y sin guard, hermana de
`auth/signin`. Nueva carpeta `features/catalog/` con `catalog-page`, `catalog.models`,
`catalog-api.service` (sobre `HttpBackend`, sin interceptores de token/branch) y
`cart.service` (signals + `computed` total, persistido en `localStorage`).
En el admin: subida de imagen en la ficha del plato; en la sucursal, campo de slug +
URL pública + QR (`<canvas>`, descarga PNG; dependencia `qrcode` a aprobar).

| Ruta | Componente | Acceso |
|---|---|---|
| `/carta/:slug` | `CatalogPage` | Público, sin shell |

## Criterios de aceptación

- [ ] `GET /api/v1/public/catalog/{slug}` responde sin token; `404` con slug inexistente.
- [ ] Un plato oculto en la sucursal (`menu_item_branch_availability`) no aparece en la
      carta de esa sucursal; con override disponible, sí.
- [ ] Los ítems traen nombre de categoría, descripción, precio, URL de imagen (o `null`)
      y la lista de ingredientes por nombre, sin unidades.
- [ ] `PUT /menu-items/{id}/image` sin token → 401; con token y archivo válido → 200 y
      la imagen se sirve en `/media/menu/...`; archivo no-imagen o > 2 MB → 400.
- [ ] `PUT /branches/{id}/public-slug` con slug de otra sucursal → 409; formato inválido
      → 409 `branch.invalid_slug`.
- [ ] Frontend: la carta filtra por categoría y busca por texto/ingrediente; el carrito
      suma el total estimado y sobrevive a recargar la página.
- [ ] El QR del admin apunta a `/carta/<slug>` y abre la carta al escanearlo.

## Pruebas

- **Domain:** `MenuItem.SetImage/ClearImage`; `Branch.SetPublicSlug` (formato, `null`).
- **Application:** `GetPublicCatalogHandler` (disponibilidad efectiva, ingredientes por
  nombre, categorías con ítems visibles, slug inexistente), `SetMenuItemImageHandler`
  (tipo/tamaño, reemplazo con borrado del anterior), `ClearMenuItemImageHandler`,
  `SetBranchPublicSlugHandler` (conflicto, limpieza).
- **Frontend (Vitest):** `cart.service` (add/inc/dec/remove/clear, total, persistencia);
  `catalog-page` (`filtered` por categoría y texto, card con y sin imagen).

## Ramas/PR

- `feat/fase-11a-carta-publica-backend` → `feat(fase-11a): carta pública por sucursal +
  almacenamiento de imágenes de plato`
- `feat/fase-11b-carta-publica-frontend` → `feat(fase-11b): frontend de carta pública
  (QR, filtros, carrito) + subida de imágenes de plato`

## Estado

- Fase 11a: backend implementado (dominio, almacenamiento, endpoints admin y público,
  migración, semilla, pruebas de dominio y aplicación).
- Fase 11b: pendiente.
