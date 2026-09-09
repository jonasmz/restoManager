# Fase 1 — Auth y usuarios

## Objetivo y alcance

Login funcional de extremo a extremo:

- **Auth API** sobre `resto_identity`: alta de usuarios (solo admin), login,
  refresh (con rotación), logout, gestión de roles. Emite **JWT firmados con clave
  asimétrica RSA** y publica `/.well-known/jwks.json` +
  `/.well-known/openid-configuration`.
- **Business API**: valida el JWT contra el JWKS de la Auth API; expone un endpoint
  protegido de prueba (`GET /api/v1/me`) y la base de policies.
- **Frontend**: pantalla `signin` (del template), `AuthService`, interceptor que
  adjunta el bearer y renueva ante `401`, guard de rutas, y arranque del selector de
  sucursal (placeholder hasta Fase 2).

Ver [`../auth.md`](../auth.md) para el detalle de diseño.

## Dependencias

Fase 0.

## Decisiones (resueltas 2026-09-08)

| # | Decisión |
|---|---|
| 1 | **5 roles**: `ADMIN`, `BRANCH_MANAGER`, `WAITER`, `KITCHEN`, `INVENTORY`. Autorización por **policies** en la Business API; el/los rol(es) viajan en el claim `role` del JWT. Matriz permiso×área más abajo (borrador a confirmar al implementar). |
| 2 | **Access token: 15 min. Refresh token: 14 días, de un solo uso (rotación).** Cada `/auth/refresh` emite un refresh nuevo e invalida el anterior; presentar un refresh ya consumido **revoca toda la cadena** de esa sesión (detección de robo). |
| 3 | **Frontend guarda access y refresh en `localStorage`.** Sin cookies ni CSRF. Interceptor trivial. (Aceptado el riesgo XSS para una herramienta interna tras login; mitigación: access token corto + CSP en el frontend.) |
| 4 | **No hay `/auth/register` público.** Un `ADMIN` (o `BRANCH_MANAGER` para su sucursal) crea el usuario y lo **vincula obligatoriamente a una fila de `employees`**. No existen usuarios sin `employee` en esta versión. |
| 5 | **Vínculo `AppUser` ↔ `employees`**: `AppUser.EmployeeId` (int, requerido). Al emitir el token se ponen los claims `employee_id` y `branch_id` (= `employees.branch_id`; se admite `branch_id` múltiple como lista para futuros casos multi-sucursal). `DOM-06` se valida con esos claims. |
| 6 | **Firma RSA 2048**, JWT con `kid` en el header. La Auth API soporta **dos claves activas** a la vez (actual + siguiente) para rotar sin downtime; ambas se publican en el JWKS. Clave privada montada como secreto (fichero) vía `.env` / secrets de compose, nunca en el repo. |

### Matriz de permisos — BORRADOR (confirmar al implementar la fase)

Áreas = módulos del roadmap. `RW` = leer y escribir · `R` = solo lectura · `—` = sin
acceso. Todo acceso a datos de sucursal se restringe además a la(s) sucursal(es) del
claim `branch_id` (`DOM-06`), salvo `ADMIN`.

| Área | ADMIN | BRANCH_MANAGER | WAITER | KITCHEN | INVENTORY |
|---|---|---|---|---|---|
| Usuarios, roles, config. empresa | RW | — | — | — | — |
| Sucursales, departamentos | RW | R (la suya) | — | — | — |
| Empleados, turnos, ausencias | RW | RW (su sucursal) | — | — | — |
| Menú, categorías, recetas, impuestos | RW | RW | R | R | R |
| Estaciones de cocina | RW | RW | — | R | — |
| Inventario, movimientos, mermas | RW | RW | — | — | RW |
| Proveedores, órdenes de compra, recepción | RW | RW | — | — | RW |
| Mesas y estado operativo | RW | RW | RW | — | — |
| Sesiones de mesa (abrir/cerrar) | RW | RW | RW | — | — |
| Reservas | RW | RW | RW | — | — |
| Pedidos MESA / BARRA / TAKEAWAY (crear, ítems, cerrar) | RW | RW | RW | R (comanda) | — |
| Pedidos DELIVERY y despacho | RW | RW | RW | R | — |
| Cocina: marcar ítems preparados | RW | RW | — | RW | — |
| Pagos y cobro | RW | RW | RW | — | — |
| Descuentos (catálogo) | RW | RW | — | — | — |
| Clientes, gift cards, reseñas | RW | RW | RW (alta/consulta) | — | — |
| Dashboard y reportes | RW | R (su sucursal) | — | — | R (inventario) |

## Backend — Auth API

- `AppUser : IdentityUser<int>` con `EmployeeId` (int, requerido);
  `AppRole : IdentityRole<int>` sembrado con los 5 roles fijos.
  `AddIdentityCore` + stores EF sobre `AuthDbContext` / `resto_identity`.
- Migración inicial de Identity + `RefreshToken`s + seed de roles.
- Endpoints (`/auth`):
  - `POST /auth/login` → `{ accessToken, refreshToken, expiresIn }`.
  - `POST /auth/refresh` → rota el refresh (invalida el anterior; reuso ⇒ revoca la
    cadena) y devuelve un par nuevo.
  - `POST /auth/logout` → revoca el refresh presentado.
  - `GET/POST/PUT /auth/users` (solo `ADMIN`/`BRANCH_MANAGER`) → alta de usuario
    **con `employeeId` obligatorio**, asignación de rol(es), reseteo de contraseña,
    activar/desactivar.
  - `GET /auth/roles` → los 5 roles (catálogo fijo, sin CRUD en esta versión).
- Emisión de JWT: puerto `ITokenIssuer` en `Application`, implementación en
  `Infrastructure` que firma con RSA 2048 (clave activa por `kid`). Claims:
  `sub`, `email`, `role` (uno o varios), `employee_id`, `branch_id` (lista).
  `iss`/`aud`/`exp` (15 min) estándar.
- `RefreshToken`: entidad en `resto_identity` con `tokenHash`, `sessionId`
  (agrupa la cadena rotada), `expiresAt` (14 días), `consumedAt`, `revokedAt`.
- `GET /.well-known/jwks.json` (claves públicas activas) y `openid-configuration`
  (issuer + `jwks_uri` + algoritmos).
- Gestión de claves: `RSA_SIGNING_KEY_CURRENT` / `RSA_SIGNING_KEY_NEXT` por
  configuración (rutas a ficheros PEM montados como secreto).

## Backend — Business API

- Activar `AddJwtBearer` con `Authority` = URL interna de la Auth API
  (`http://backend-auth:8080`) y `Audience`; cachea el JWKS y refresca ante `kid`
  desconocido. No llama a la Auth API por request.
- Puerto `ICurrentUser` (`UserId`, `Roles`, `EmployeeId`, `BranchIds`) resuelto
  desde el `ClaimsPrincipal`.
- `GET /api/v1/me` (protegido) → devuelve los claims; verifica la cadena completa.
- Registro de **policies** con los 5 roles; en Fase 1 solo se definen
  `RequireAuthenticated` y un `RequireAdmin` de ejemplo. Las policies por área se
  añaden en cada fase siguiendo la matriz de arriba.

## Frontend

- `features/auth/sign-in` — portar `signin.html` del template (card centrada,
  `needs-validation`). **No hay pantalla de registro** (alta solo por admin); el
  placeholder `sign-up` de Fase 0 se elimina o se convierte en "alta de usuario"
  dentro de la administración (Fase 2).
- `core/auth/auth.service`: `login`, `logout`, `refresh`, señal `currentUser`
  (decodifica el JWT para `roles`/`employeeId`/`branchIds`). Tokens en
  `localStorage` (claves `rm.access` / `rm.refresh`).
- `core/http/auth.interceptor`: añade `Authorization: Bearer` a las llamadas a la
  Business API; ante `401` llama a `refresh` **una sola vez** (con cola de requests
  en espera) y reintenta; si el refresh falla, limpia el storage y navega a
  `/auth/signin`.
- `core/auth/auth.guard` (functional guard): protege las rutas hijas del
  `LayoutComponent`; `role.guard` opcional para ocultar/mostrar secciones según
  `roles`.
- El menú lateral filtra sus grupos según el rol del usuario.

## Criterios de aceptación

- [ ] Un `ADMIN` crea un usuario con `employeeId`; sin `employeeId` el alta se
      rechaza (`422`).
- [ ] `POST /auth/login` devuelve access (15 min) + refresh (14 días); el access es
      un JWT RSA con `kid` y contiene `role`, `employee_id`, `branch_id`.
- [ ] `/auth/refresh` rota el refresh: el anterior deja de servir; reutilizar uno ya
      consumido revoca la cadena y obliga a re-login.
- [ ] `/.well-known/jwks.json` publica la(s) clave(s) pública(s); la Business API
      valida tokens sin llamar a la Auth API por request.
- [ ] `GET /api/v1/me` → `200` con token válido, `401` sin token, `401` con token
      expirado.
- [ ] Un token con rol insuficiente recibe `403` en el endpoint protegido por
      `RequireAdmin`.
- [ ] Frontend: login entra al shell; recargar la página mantiene la sesión (tokens
      en `localStorage`); el interceptor renueva de forma transparente; logout y
      fallo de refresh redirigen a `signin`.
- [ ] El menú lateral solo muestra lo permitido por el rol.

## Pruebas

- Unit: emisión de token (claims, `exp`, `kid`), rotación de refresh (invalida el
  anterior, detecta reuso), hashing de refresh.
- Integración: `login` → `me`; `refresh` feliz y con reuso; `jwks` accesible;
  token expirado; alta de usuario sin `employeeId` rechazada.
- Frontend: `AuthService` (login/refresh/logout), interceptor (cola en `401`,
  redirección al fallar), guard.

## Ramas/PR

1. `feat/fase-01a-auth-api` — Identity, usuarios, login/refresh/logout, JWT + JWKS.
   **✅ Implementado (PR #3).** Hexágono completo (Domain/Application/Infrastructure/
   Api), migración `InitialIdentity`, rotación de refresh con detección de reuso,
   `RS256` + JWKS, `/auth/users` y `/auth/roles` protegidos con `RequireAdmin`,
   admin de arranque en Development. Tests unitarios de `TokenService`. Verificado de
   extremo a extremo contra el compose (login → refresh → reuso → 401; alta de
   usuario; 422/409/401). Tests de integración con Postgres: pendientes (1b/1c).
2. `feat/fase-01b-business-jwt` — validación en la Business API, `ICurrentUser`,
   policies base, `/api/v1/me`. **✅ Implementado (PR #4, apilado sobre 1a).**
   `AddJwtBearer` con descubrimiento OIDC contra `http://backend-auth:8080`;
   `ICurrentUser` (`CurrentUserClaims.FromPrincipal`) acepta `branch_id` escalar o
   array; policy `RequireAdmin`; `/api/v1/me` y `/api/v1/admin-check` de prueba.
   El emisor lógico pasa a ser la dirección de red del servicio
   (`Auth__Issuer=http://backend-auth:8080` en compose) para que `iss` + `jwks_uri`
   sean coherentes y alcanzables. 5 tests unitarios; verificado 401/200/403 de
   extremo a extremo.
3. `feat/fase-01c-auth-frontend` — `sign-in`, `AuthService`, interceptor, guards.
