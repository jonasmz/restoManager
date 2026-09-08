# Fase 1 — Auth y usuarios

## Objetivo y alcance

Login funcional de extremo a extremo:

- **Auth API** sobre `resto_identity`: registro/alta de usuarios, login, refresh,
  logout, gestión de roles. Emite **JWT firmados con clave asimétrica** y publica
  `/.well-known/jwks.json` + `/.well-known/openid-configuration`.
- **Business API**: valida el JWT contra el JWKS de la Auth API; expone un endpoint
  protegido de prueba (`GET /api/v1/me`).
- **Frontend**: pantallas `signin` y `signup` (del template), `AuthService`,
  interceptor que adjunta el bearer y renueva ante `401`, guard de rutas, y arranque
  del selector de sucursal (placeholder hasta Fase 2).

Ver [`../auth.md`](../auth.md) para el detalle de diseño.

## Dependencias

Fase 0.

### Decisiones bloqueantes (resolver con el usuario antes de implementar)

1. **Catálogo de roles** y **matriz de permisos** por módulo/endpoint.
2. Vida del **access token** y del **refresh token**; ¿rotación de refresh con
   detección de reuso?
3. **Almacenamiento del token en el frontend**: `localStorage` vs cookie `httpOnly`
   + CSRF.
4. **Registro**: ¿`/auth/register` público o alta de usuarios solo por un
   administrador?
5. Forma del **vínculo `AppUser` ↔ `employees`**: claim `employee_id` +
   `branch_id`(s) en el token; cómo se asigna al crear el usuario.
6. Algoritmo de firma (RSA 2048 vs ECDSA P-256) y gestión/rotación de `kid`.

## Backend — Auth API

- `AppUser : IdentityUser<int>`, `AppRole : IdentityRole<int>`;
  `AddIdentityCore` + stores EF sobre `AuthDbContext` / `resto_identity`.
- Migración inicial de Identity.
- Endpoints (`/auth`): `register` (según decisión 4), `login`, `refresh`, `logout`,
  `roles` (CRUD, solo admin), `users` (alta/asignación de rol y de sucursal).
- Emisión de JWT: servicio `ITokenIssuer` (puerto en `Application`, implementación en
  `Infrastructure`) que firma con la clave privada montada como secreto. Claims:
  `sub`, `email`, `role`(s), `employee_id`, `branch_id`(s).
- Refresh tokens: entidad `RefreshToken` (hash, expiración, revocado) en
  `resto_identity`.
- `GET /.well-known/jwks.json` y `openid-configuration`.

## Backend — Business API

- `AddJwtBearer` con `Authority`/`MetadataAddress` → Auth API; cachea el JWKS.
- Middleware que expone el `ClaimsPrincipal` a `Application` (puerto
  `ICurrentUser` con `UserId`, `Roles`, `EmployeeId`, `BranchIds`).
- `GET /api/v1/me` (protegido) devuelve los datos del token — sirve para verificar la
  cadena completa.
- Base de **policies** de autorización (vacía salvo `RequireAuthenticatedUser`);
  se llena en fases siguientes según la matriz de la decisión 1.

## Frontend

- `features/auth/signin`, `features/auth/signup` — portar `signin.html` /
  `signup.html` del template (card centrada, `needs-validation`, validación de
  coincidencia de contraseñas).
- `core/auth/auth.service`: `login`, `logout`, `refresh`, señal `currentUser`.
- `core/http/auth.interceptor`: añade `Authorization: Bearer`; ante `401` intenta
  `refresh` una vez y reintenta; si falla, navega a `signin`.
- `core/auth/auth.guard`: protege las rutas dentro del `LayoutComponent`.
- Almacenamiento del token según decisión 3.

## Criterios de aceptación

- [ ] Alta de usuario (o registro) crea el usuario en `resto_identity` con su rol.
- [ ] `POST /auth/login` devuelve access + refresh token; el access token es un JWT
      firmado con clave asimétrica y contiene `role`, `employee_id`, `branch_id`.
- [ ] `/.well-known/jwks.json` sirve la clave pública; la Business API valida tokens
      sin llamar a la Auth API por request.
- [ ] `GET /api/v1/me` responde `200` con token válido y `401` sin él.
- [ ] En el frontend: login exitoso entra al shell; logout y expiración redirigen a
      `signin`; el interceptor renueva el token de forma transparente.
- [ ] Un token con rol insuficiente recibe `403` (probado con una policy de ejemplo).

## Pruebas

- Unit: emisión de token (claims correctos), validación de refresh, hashing.
- Integración: `login` → `me` con el token real; `jwks` accesible; expiración.
- Frontend: `AuthService` y el interceptor (con `HttpTestingController`).

## Ramas/PR

PR único `feat/fase-01-auth`, salvo que la decisión 1 (roles/permisos) se quiera
separar en `feat/fase-01-roles`.
