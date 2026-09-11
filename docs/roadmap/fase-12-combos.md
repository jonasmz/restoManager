# Fase 12 — Combos

## Objetivo y alcance

Ofrecer **combos**: agrupar varios platos (con o sin bebida) y venderlos con un
**descuento plano**. Cada combo se define con **un** modo de precio:

- **`PercentageOff`** — un porcentaje sobre la suma de los precios de sus platos
  (ej. "cono de papas grande + coca 500ml, 10%").
- **`FixedTotal`** — un precio fijo (ej. "mila completa + quilmes 750ml, $34000, antes
  $42000").

Sin condiciones de medio de pago, sin grupos de elección, sin vigencia por fechas
(es un catálogo, se prende/apaga con `IsAvailable`). El combo es un producto propio del
menú: se administra desde el ABM de menú, se usa en el POS y se muestra en la carta
pública.

Alcance nuevo, posterior al roadmap 0–11.

## Dependencias

- Fase 4 (menú: `menu_items`, precios impuestos-incluidos, disponibilidad por sucursal).
- Fase 6 (pedidos: `orders`, `order_items`, `order_discounts`, consumo de stock por receta).
- Fase 11 (carta pública: `GetPublicCatalogHandler`, `/api/v1/public/catalog/{slug}`,
  almacén de imágenes `IImageStorage` + `/media/menu`).

## Decisiones bloqueantes — RESUELTAS (2026-09-10)

1. **Modos de precio:** `PercentageOff` (% sobre la suma de los platos) o `FixedTotal`
   (precio fijo). Uno por combo. Catálogo de estado `combos.price_mode`:
   `PERCENTAGE_OFF` / `FIXED_TOTAL`. **Sin** condición de medio de pago.
2. **En el pedido, el combo se expande en líneas.** Al agregar un combo se cargan las
   líneas de cada plato a su **precio de lista** (`menu_items.price`) y una fila de
   **"ahorro del combo" congelada**. El subtotal del pedido muestra el "antes"; el ahorro
   se resta aparte. Así el consumo de stock y los reportes siguen operando sobre líneas
   reales (`order_items.menu_item_id`) sin tocar nada.
3. **Composición: conjunto fijo** de `(plato, cantidad)`. Sin "elegí 1 de este grupo".
4. **Disponibilidad por sucursal derivada, sin tabla propia.** Un combo se ofrece en una
   sucursal si `combos.is_available` **y** todos sus platos están disponibles ahí
   (override `menu_item_branch_availability` ?? `menu_items.is_available`).
5. **Los combos se muestran en la carta pública** (`/carta/:slug`), en su propia sección,
   con "antes/ahora" o badge `X% OFF`.
6. **El ahorro del combo cuenta como descuento en los reportes.** `ReportQueries` suma
   `order_combos.savings` al total de "descuentos"; la venta efectiva ya sale bien porque
   usa `orders.total_amount`.
7. Un plato que ya está como componente de un combo del pedido **no** se fusiona con un
   alta suelta del mismo plato: siguen siendo líneas separadas.

## Desviaciones del esquema

Como en Fase 4 (`menu_item_branch_availability`) y Fase 11 (`menu_items.image_key`,
`branches.public_slug`), esta fase añade tablas y columnas fuera de
`requirements/restaurant_schema.sql` — migración EF **`Fase12Combos`**:

- **`combos`** — `id`, `name varchar(100)`, `description varchar(255)`,
  `image_key varchar(200) NULL`, `price_mode varchar(20) NOT NULL`
  (`PERCENTAGE_OFF` | `FIXED_TOTAL`), `price_value decimal(10,2) NOT NULL`
  (% `0 < v <= 100` o monto `> 0` según el modo), `is_available boolean NOT NULL`.
- **`combo_items`** — `id`, `combo_id`, `menu_item_id`, `quantity int NOT NULL`,
  `UNIQUE (combo_id, menu_item_id)`.
- **`order_combos`** — `id`, `order_id`, `combo_id`, `combo_name varchar(100) NOT NULL`
  (snapshot), `list_price decimal(10,2) NOT NULL` ("antes", congelado),
  `savings decimal(10,2) NOT NULL` (ahorro, congelado).
- **`order_items.order_combo_id int NULL`** — FK a `order_combos`; si no es nulo, la
  línea es un componente de ese combo.

FKs sin cascada (NO ACTION en BD; `ClientCascade` en EF para los hijos de agregados),
igual que el resto del modelo.

## Backend

### Dominio — módulo Menú (`Domain/Menu/ComboEntities.cs`)

- `enum ComboPriceMode { PercentageOff, FixedTotal }` + `ToDbValue`/`FromDbValue`/
  `TryFromDbValue`.
- **`Combo`** (agregado): `Id`, `Name`, `Description`, `string? ImageKey`,
  `ComboPriceMode PriceMode`, `decimal PriceValue`, `bool IsAvailable = true`,
  `IReadOnlyList<ComboItem> Items`.
  - `Update(name, description, priceMode, priceValue, isAvailable)` — valida el rango de
    `priceValue` según el modo (`menu.combo_invalid_percentage` para `PercentageOff` con
    `v <= 0` o `v > 100`; `menu.combo_invalid_price` para `FixedTotal` con `v <= 0`).
  - `SetImage(key)` / `ClearImage()` — espejo de `MenuItem` (Fase 11).
  - `SetItems(IEnumerable<(int menuItemId, int quantity)>)` — reemplazo en bloque; rechaza
    lista vacía (`menu.combo_no_items`), `menuItemId` duplicado
    (`menu.combo_duplicate_item`), `quantity <= 0` (`menu.combo_invalid_quantity`).
- **`ComboItem`**: `Id`, `ComboId`, `MenuItemId`, `Quantity`;
  `internal static Create(menuItemId, quantity)`.

### Dominio — módulo Ventas (`Domain/Sales/SalesEntities.cs`)

- **`OrderCombo`** (hijo de `Order`): `Id`, `OrderId`, `ComboId`, `string ComboName`
  (snapshot), `decimal ListPrice` (Σ precios de lista, congelado), `decimal Savings`
  (ahorro, congelado). `internal static Create(comboId, name, listPrice, savings)`.
- `OrderItem`: nueva propiedad `int? OrderComboId` + navegación CLR interna al
  `OrderCombo` para que EF fije la FK al guardar (el `OrderCombo` se inserta primero).
- `Order`:
  - `private readonly List<OrderCombo> _combos = [];` + `IReadOnlyList<OrderCombo> Combos`.
  - `decimal ComboSavingsTotal => Money.Round(_combos.Sum(c => c.Savings));`
  - `Recalculate()` → `TotalAmount = max(0, round(ItemsSubtotal − DiscountTotal − ComboSavingsTotal))`.
  - **`AddCombo(Combo combo, IReadOnlyDictionary<int,decimal> menuItemPrices, int quantity)`**:
    `EnsureOpen`; `quantity >= 1`; crea el `OrderCombo`; por cada `ComboItem` crea un
    `OrderItem` (`MenuItemId`, `Quantity = comboItemQty * quantity`,
    `UnitPrice = menuItemPrices[menuItemId]`, ligado al `OrderCombo`), lo agrega a
    `_items`; `listPrice = Σ LineTotal`; `savings` según el modo:
    `PercentageOff` → `round(listPrice * priceValue / 100)`;
    `FixedTotal` → `round(listPrice − priceValue * quantity)`, **clamp a `[0, listPrice]`**
    (un combo nunca es recargo); `Recalculate()`.
  - **`RemoveCombo(int orderComboId)`**: `EnsureOpen`; quita las líneas del combo de
    `_items` y la fila de `_combos`; `Recalculate()`.
  - `UpdateItem` / `RemoveItem`: si `FindItem(id).OrderComboId is not null` →
    `DomainRuleException("sales.item_in_combo", "Esa línea es parte de un combo; quitá el combo entero.")`.
  - `AddItem`: agregar `&& i.OrderComboId is null` al predicado de fusión de líneas.

### Persistencia

- `MenuConfig.cs`: `ComboConfig` (name 100 req, description 255 req, `ImageKey` 200
  nullable, `PriceMode` string-conv 20, `PriceValue` `.Money()`,
  `HasMany(Items).WithOne().HasForeignKey(ComboId).OnDelete(ClientCascade)` +
  `Navigation(Items).UsePropertyAccessMode(Field)`); `ComboItemConfig` (`Quantity` int,
  `UNIQUE(ComboId, MenuItemId)`, `Fk<ComboItem, MenuItem>`).
- `SalesConfig.cs`: `OrderComboConfig` (`ComboName` 100, `ListPrice`/`Savings` `.Money()`,
  `Fk<OrderCombo, Order>` `ClientCascade` + field access como los otros hijos de `Order`,
  `Fk<OrderCombo, Combo>`); en `OrderItemConfig`, relación opcional
  `HasOne(OrderCombo).WithMany().HasForeignKey(OrderComboId).IsRequired(false)`.
- `BusinessDbContext`: DbSets `Combos`, `ComboItems`, `OrderCombos`.
- `IComboRepository` (`Domain/Menu/Repositories.cs`): `GetAsync(id)` (con `Items`),
  `ListAsync(search, skip, take)`, `CountAsync(search)`, `ListAvailableAsync()` (carta),
  `Add`. Impl en `MenuRepositories.cs`; registrar en `Infrastructure/DependencyInjection.cs`.

### Aplicación

- `Application/Menu/Combos/ComboUseCases.cs`:
  - DTOs `ComboItemDto(int MenuItemId, int Quantity)`,
    `ComboDto(int Id, string Name, string Description, string PriceMode, decimal
    PriceValue, bool IsAvailable, IReadOnlyList<ComboItemDto> Items, string? ImageUrl)`,
    `ComboPriceDto(int ComboId, decimal ListPrice, decimal Price, decimal Savings,
    decimal? PercentOff)`.
  - `SaveComboCommand` + `SaveComboValidator` (name, `priceMode` válido, `priceValue > 0`,
    cada item `MenuItemId > 0` / `Quantity > 0`) + `SaveComboHandler` (verifica que cada
    `MenuItemId` existe; `Update` + `SetItems`).
  - `ListCombosHandler` / `GetComboHandler` / `GetComboPriceHandler` (calcula
    list/price/savings/percentOff — apoyo del preview del admin, espejo de
    `GetMenuItemCostHandler`).
  - `SetComboImageHandler` / `ClearComboImageHandler` — reutilizan `IImageStorage` +
    `MenuImagePath` (jpeg/png/webp ≤ 2 MB, igual que `SetMenuItemImageHandler`).
- `Application/Sales/Orders/OrderComboUseCases.cs`:
  - `AddOrderComboCommand(int OrderId, int ComboId, int Quantity)` + `AddOrderComboHandler`:
    `access.EnsureCanOperate(order.BranchId)`; carga el combo con items; resuelve la
    disponibilidad de cada plato en la sucursal (`IMenuItemAvailabilityRepository` ??
    `MenuItem.IsAvailable`) → si alguno no está disponible,
    `DomainRuleException("sales.combo_component_unavailable", …)`; arma `menuItemPrices`
    (`IMenuItemRepository`); `order.AddCombo(...)`. Devuelve `orderCombo.Id`.
  - `RemoveOrderComboCommand(int OrderId, int OrderComboId)` + handler → `order.RemoveCombo`.
  - `OrderDto` gana `IReadOnlyList<OrderComboDto> Combos` (`Id, ComboId, ComboName,
    ListPrice, Savings`) y `decimal ComboSavingsTotal`; `OrderItemDto` gana
    `int? OrderComboId`.
- Registrar todos los handlers en `Application/DependencyInjection.cs`.

### API

- Grupo `/api/v1` (policy `MenuAccess`), en `MenuEndpoints.cs` + `MenuContracts.cs`:
  `GET /combos?search&page&pageSize`, `GET /combos/{id}`, `GET /combos/{id}/price`,
  `POST /combos`, `PUT /combos/{id}`, `PUT /combos/{id}/image` (`.DisableAntiforgery()`),
  `DELETE /combos/{id}/image`. Body
  `SaveComboRequest(string Name, string Description, string PriceMode, decimal PriceValue,
  bool IsAvailable, IReadOnlyList<ComboItemBody> Items)`,
  `ComboItemBody(int MenuItemId, int Quantity)`.
- Grupo `/api/v1` (policy `SalesAccess`), en `SalesEndpoints.cs` + `SalesContracts.cs`:
  `POST /orders/{id:int}/combos` (body `AddOrderComboRequest(int ComboId, int Quantity)`)
  → `201` + `CreatedIdResponse`; `DELETE /orders/{id:int}/combos/{orderComboId:int}` → `204`.

### Carta pública

- `PublicCatalogContracts.cs`: `PublicCatalogDto` gana
  `IReadOnlyList<PublicComboDto> Combos`; `PublicComboDto(int Id, string Name, string
  Description, string? ImageUrl, IReadOnlyList<PublicComboItemDto> Items /* {Name, Quantity} */,
  decimal ListPrice, decimal Price, decimal? PercentOff)`.
- `GetPublicCatalogHandler`: además de los platos, carga `combos.ListAvailableAsync()`,
  filtra por `combo.IsAvailable` **y** todos los componentes disponibles en la sucursal
  (reusa el dict `overrides` + `MenuItem.IsAvailable`), resuelve nombres/precios de los
  componentes, calcula `ListPrice` / `Price` / `PercentOff`. El endpoint no cambia.

### Reportes

- `Infrastructure/.../Reports/ReportQueries.cs`: sumar `order_combos.savings` junto con
  `order_discounts.applied_amount` en el total de "descuentos" de `SalesSummaryAsync` (y
  donde se agregue descuentos).

### Consumo de stock

Sin cambios: las líneas de componente llevan `MenuItemId` real, así que
`SaleConsumptionService.PostForOrderAsync` expande sus recetas como siempre.

### Semilla

`BusinessDevSeeder`: 1–2 combos demo con los platos ya sembrados (uno `FixedTotal`, uno
`PercentageOff`), idempotente.

## Frontend

| Ruta | Contenido | Cómo construirla |
|---|---|---|
| `menu/combos` | Lista de combos (nombre, modo, precio/‰, disponible, miniatura; botón "Nuevo") | tabla + `menu-items-page.ts` como referencia |
| `menu/combos/:id` | ABM de un combo: datos + `items` FormArray `(plato, cantidad)` + tarjeta de imagen + preview "Antes / Precio combo / Ahorro" | modelado sobre `menu-item-detail-page.ts`; preview desde `GET /combos/{id}/price` |
| `/carta/:slug` | Sección "Combos" en la carta pública (imagen, platos incluidos, precio antes/ahora o badge `X% OFF`) | reusa las cards de `catalog-page.ts` |

- `app.routes.ts`: `menu/combos` y `menu/combos/:id` con `canActivate: [menuRoles]`,
  hermanas de `menu/items`. `nav-items.ts`: ítem "Combos" en el grupo "Menú"
  (icono `ti-basket`).
- `menu-api.service.ts`: `listCombos`, `getCombo`, `saveCombo`, `comboPrice`,
  `uploadComboImage`, `deleteComboImage`. `menu.models.ts`: `Combo`, `ComboItem`,
  `SaveComboBody`, `ComboPrice`.
- POS (`order-page.ts`): sección "Combos" en el panel "Carta" (`menuApi.listCombos()`);
  card → `addCombo(combo)` → `salesApi.addCombo(orderId, {comboId, quantity: 1})` →
  `reload()`. En "Cuenta": bloque por `OrderCombo` (nombre, "antes" tachado, "ahorro −$X")
  con las líneas de componente **anidadas y de solo lectura** (sin +/−, sin tacho); un
  botón "Quitar combo" → `salesApi.removeCombo(orderId, orderComboId)`. "Totales": renglón
  "Ahorro combos −$X". `sales.models.ts` / `sales-api.service.ts` ganan las formas y
  métodos de combo (`Order.combos`, `Order.comboSavingsTotal`, `OrderItem.orderComboId`).
- Carta pública (`catalog-page.ts` + `catalog.models.ts`): render de la sección "Combos".

## Criterios de aceptación

- [ ] ABM de combo: crear/editar con modo `PercentageOff` (%) o `FixedTotal` (precio),
      lista de platos con cantidad, imagen opcional; el preview muestra "antes / precio /
      ahorro" coherente con `GET /combos/{id}/price`.
- [ ] `POST /orders/{id}/combos` agrega una línea por cada plato del combo (a precio de
      lista) + una fila `order_combos` con `list_price` y `savings` congelados; el total
      del pedido baja exactamente el ahorro.
- [ ] `PercentageOff` 10% sobre suma $X → ahorro `round(X*0.10)`; `FixedTotal` $34000
      sobre suma $42000 → ahorro $8000; combo cuyo precio fijo ≥ suma → ahorro 0.
- [ ] `DELETE /orders/{id}/combos/{orderComboId}` quita las líneas del combo y su fila; el
      total vuelve a subir.
- [ ] Editar o quitar una línea que es parte de un combo → `409 sales.item_in_combo`.
- [ ] Agregar suelto un plato que ya está como componente de un combo → línea separada
      (no fusiona).
- [ ] Cobrar un pedido con combo → se descuentan de stock los ingredientes de todos los
      platos del combo (una vez, idempotente por `(ORDER, order.id)`).
- [ ] `GET /api/v1/public/catalog/{slug}` incluye `combos[]` con `listPrice` / `price` /
      `percentOff`; un combo desaparece de la carta de una sucursal si alguno de sus platos
      está oculto ahí.
- [ ] El reporte de ventas del período suma el ahorro de combos al total de "descuentos".
- [ ] `AddOrderCombo` con un plato no disponible en la sucursal →
      `409 sales.combo_component_unavailable`.

## Pruebas

- **Domain Menú:** `Combo.Update` (% rango, fijo ≤ 0), `SetItems` (vacío, duplicado,
  qty ≤ 0), `SetImage`/`ClearImage`.
- **Domain Ventas:** `Order.AddCombo` (N líneas + `OrderCombo`; `ItemsSubtotal` = list
  price; `ComboSavingsTotal` en ambos modos; `TotalAmount` con piso 0; `savings` clamp
  `[0, list]`), `RemoveCombo`, `UpdateItem`/`RemoveItem` sobre línea de combo →
  `sales.item_in_combo`, `AddItem` no fusiona con línea de combo.
- **Application:** `AddOrderComboHandler` (fijo y %, componente no disponible → error),
  `GetComboPriceHandler`, `GetPublicCatalogHandler` (combo oculto si un componente no está
  disponible en la sucursal; list/price/percentOff correctos).
- **Frontend:** el proyecto no tiene specs; se valida con `ng lint` + `ng build` limpios
  y prueba manual en el navegador.

## Ramas/PR

- `feat/fase-12a-combos-backend` → `feat(fase-12a): combos — dominio, ABM, integración con
  el pedido y carta pública`.
- `feat/fase-12b-combos-frontend` → `feat(fase-12b): frontend de combos — ABM, uso en el
  POS y carta pública`. Si 12b queda grande, partir POS vs. carta pública en 12b/12c.

## Estado

**Pendiente.** Spec lista (2026-09-10). No implementada.
