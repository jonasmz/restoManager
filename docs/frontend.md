# Frontend (Angular 22)

## Principio rector: fidelidad al template

El frontend **reproduce fielmente** el template alojado en
`requirements/inapp/` (proyecto Vite "InApp" de CodesCandy, Bootstrap 5.3). No se
diseña una interfaz nueva: se porta la del template a Angular, conservando layout,
espaciado, colores, tipografía, iconos y comportamiento.

Antes de construir una pantalla, **abre la página equivalente** en
`requirements/inapp/src/*.html` y sus estilos en
`requirements/inapp/src/assets/scss/`.

## Stack y versiones

| Pieza | Elección |
|---|---|
| Framework | Angular **22** (standalone, señales, nuevo control flow) |
| CSS | **Bootstrap 5.3** compilado desde SCSS (mismos overrides del template) |
| Iconos | **`@tabler/icons-webfont`** — clases `ti ti-*`, idénticas al template |
| Gráficas | **`ng-apexcharts`** (wrapper de ApexCharts) |
| Tipografía | **Poppins** (Google Fonts), igual que el template |
| Lint/format | ESLint (`angular-eslint`) + Prettier |
| Tests | Runner por defecto de Angular 22 (**Vitest**); e2e fuera de alcance por ahora |

> Nota: el template también trae `bootstrap-icons` como dependencia pero **no la
> usa** en el markup. No la incluyas.

## Estructura del proyecto

```
frontend/
├── src/
│   ├── app/
│   │   ├── core/               # singletons: servicios de API, auth, interceptores, guards
│   │   │   ├── auth/
│   │   │   ├── http/           # interceptores (bearer, errores)
│   │   │   └── ...
│   │   ├── shared/             # UI reutilizable y sin estado de negocio
│   │   │   ├── ui/             # avatar, icon-shape, status-badge, kpi-card...
│   │   │   └── ...
│   │   ├── layout/             # LayoutComponent = shell (topbar + sidebar + overlay)
│   │   ├── features/
│   │   │   ├── dashboard/
│   │   │   ├── inventory/
│   │   │   ├── products/
│   │   │   ├── reports/
│   │   │   └── auth/           # signin, signup
│   │   ├── app.routes.ts
│   │   └── app.config.ts
│   ├── styles/                 # SCSS global (ver más abajo)
│   ├── assets/
│   └── index.html
├── angular.json
└── package.json
```

## Estilos: portar el SCSS del template

El template define su tema con **overrides de variables de Bootstrap**. Se portan
esos parciales al pipeline SCSS de Angular (`angular.json` → `styles`), **no** se
reescriben:

| Parcial en `requirements/inapp/src/assets/scss/` | Contenido a conservar |
|---|---|
| `_variables.scss` | `$primary: #E66239` (naranja), `$success:#00C951`, `$info:#00B8DB`, `$warning:#F0B100`, `$danger:#FB2C36`; rampa de grises `#fafafa…#0a0a0a`; `$font-family-base: 'Poppins'`; `$font-size-base: .875rem`; `$headings-font-weight: 400`; `$min-contrast-ratio: 2.5`; escala de spacers extendida (hasta `11` = 128px); `$position-values` extra; `$icon-size-*` y `$avatar-size-*` |
| `_custom.scss` | Shell: topbar fijo 60px, sidebar 240px (colapsado 60px), `#content` con `margin-left` 240/60px, overlay móvil (`rgba(0,0,0,.45)` + blur), breakpoint `max-width: 992px` para off-canvas |
| `_avatar.scss` | `.avatar`, tamaños `avatar-xs…xxl`, indicadores `online/offline/away/busy`, `.avatar-group` |
| `_button.scss` | `.btn-icon` (botones cuadrados de icono) |
| `_icon-shape.scss` | `.icon-shape` + `.icon-xxs…xxxl` (los tiles de icono con color de las KPI cards) |
| `_border.scss` | `.border-dashed`, helpers de timeline vertical |
| `_utilities.scss` | Extensiones del utilities API de Bootstrap (`fs` responsive, posición, `translate-middle`) |

Orden de importación en el SCSS global (equivalente a `style.scss` del template):

```scss
// styles/styles.scss
@import url('https://fonts.googleapis.com/css2?family=Poppins:wght@300;400;500;600;700&display=swap');
@import '@tabler/icons-webfont/dist/tabler-icons.min.css';

@import 'variables';          // overrides ANTES de Bootstrap
@import 'bootstrap/scss/bootstrap';
@import 'avatar';
@import 'border';
@import 'button';
@import 'icon-shape';
@import 'utilities';          // usa el utilities API de Bootstrap
@import 'custom';             // shell / layout
```

- **No** usar Angular Material ni Tailwind ni otro framework de utilidades.
- Estilos específicos de componente: SCSS del componente con `ViewEncapsulation`
  por defecto; lo estructural del shell va en el SCSS global (como en el template).

## Mapeo de pantallas: template → feature Angular

| Página del template | Ruta / feature | Contenido a replicar |
|---|---|---|
| `index.html` | `/` → `dashboard` | 4 KPI cards (`icon-shape` con color primary/success/info/warning), 3 cards de profit/return/expense, gráfica de barras "Sales vs Purchase", radialBar "Customers Overview" + contadores, listas "Top Selling Products" / "Low Stock Products" / "Recent Sales" con badges de estado |
| `inventory.html` | `/inventory` | Buscador, botones Filter/Excel/PDF, `table table-hover` (Image, Code, Category, Brand, Price, Unit, Quantity, Action con iconos edit/trash), paginación en `tfoot` |
| `create-product.html` | `/products/new` | Form Bootstrap: nombre, SKU, precio, stock, categoría (`select`), imagen (`input file`), descripción (`textarea`), botones Add / Clear |
| `reports.html` | `/reports` | 4 stat cards, gráfica de área "Sales Overview" full-width con botones Randomize / Show This Year, list-group "Top Products" con imagen + unidades + revenue |
| `signin.html` | `/auth/signin` | Card centrada (max 420px), email + password + remember + forgot, validación `needs-validation` |
| `signup.html` | `/auth/signup` | Card centrada: nombre, email, password, confirmar password (validación de coincidencia), términos |
| `404-error.html` | `**` (wildcard) | Logo centrado, "404", "Page Not Found", botón a dashboard |
| Shell común (topbar + sidebar + overlay) | `LayoutComponent` | Navbar fijo con toggle desktop/móvil, campana de notificaciones con badge y dropdown, dropdown de avatar de usuario; sidebar con grupos "Main" y "Account", link activo con fondo primary translúcido |

> El template es de "inventory dashboard" genérico (marca `₹`, datos de ejemplo de
> e-commerce). Al portarlo: **mantener el diseño**, pero adaptar los textos y el
> dominio al restaurante (`requirements/restaurant_schema_specifications.md`) y el
> formato de moneda/locale al del proyecto (confirmar cuál con el usuario).

## Comportamiento del shell

Replica `requirements/inapp/src/assets/js/sidebar.js`:

- Botón desktop (`#toggleBtn`): alterna sidebar 240px ↔ 60px, `#content` acompaña.
- Botón móvil (`#mobileBtn`, `≤992px`): sidebar off-canvas + overlay a pantalla
  completa que cierra al hacer clic.
- Link activo del sidebar según la ruta actual (en Angular: `routerLinkActive`).

En Angular esto se implementa con una **señal de estado del sidebar** en un servicio
de layout, no manipulando el DOM.

## Gráficas (ApexCharts)

Portar `requirements/inapp/src/assets/js/chart.js` a componentes con `ng-apexcharts`:

| Id template | Tipo | Notas |
|---|---|---|
| `salesPurchaseChart` | `bar` agrupado | colores `#f7a085` / `#E66239` |
| `customerChart` | `radialBar` | "First Time" vs "Return", `#5BE49B` / `#E66239` |
| `salesChart` | `area` | "This Year" vs "Last Year" mensual, `#E66239` / `#198754`; botones randomize / year |

Los datos vendrán de la Business API (`MET-*` del spec: todo se deriva de datos
transaccionales, no hay tablas de agregados). Mientras no exista el endpoint, usar
datos de ejemplo claramente marcados como mock.

## Buenas prácticas Angular 22

- [ ] **Componentes standalone**; nada de `NgModule` de features.
- [ ] **Señales** (`signal`, `computed`, `linkedSignal`, `resource`) para estado;
      `toSignal` para interoperar con `HttpClient`.
- [ ] `ChangeDetectionStrategy.OnPush` en todos los componentes.
- [ ] Nuevo control flow (`@if`, `@for` con `track`, `@switch`); nada de
      `*ngIf`/`*ngFor`.
- [ ] `inject()` en vez de inyección por constructor.
- [ ] **Rutas con lazy loading** por feature (`loadComponent` / `loadChildren`).
- [ ] **Tipado estricto** (`strict: true`, `strictTemplates: true`); **prohibido
      `any`** — usar `unknown` + narrowing.
- [ ] Interfaces/typing para las respuestas de API en `core/**/models`.
- [ ] Interceptores funcionales (`HttpInterceptorFn`) para bearer y manejo de
      errores.
- [ ] Estados de **carga / error / vacío** explícitos en cada vista con datos.
- [ ] Formularios: Reactive Forms tipados; validación visual como en el template
      (`is-invalid`, `invalid-feedback`).
- [ ] Accesibilidad básica: `label` asociado a inputs, `aria-*` en botones de icono,
      foco visible.
- [ ] `lint` y `test` en verde **dentro del contenedor** (ver [`docker.md`](docker.md)).
- [ ] Añadir dependencias npm **solo tras aprobación** del usuario.
