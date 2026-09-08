# Autenticación y autorización

La autenticación corre en una **API independiente** (`backend/auth/`), separada de la
API de negocio. Ambas son proyectos .NET 10 con arquitectura hexagonal
([`backend.md`](backend.md)), pero con responsabilidades distintas.

## Reparto de responsabilidades

| | **Auth API** (`:5001`) | **Business API** (`:5002`) |
|---|---|---|
| Base de datos | `resto_identity` | `resto_business` |
| Identidad | ASP.NET Core **Identity** (usuarios, contraseñas, roles, lockout) | — (sin acceso a identidad) |
| Tokens | **Emite y firma** JWT (clave asimétrica) | **Valida** JWT como resource server |
| Endpoints | `/auth/register`, `/auth/login`, `/auth/refresh`, `/auth/logout`, gestión de usuarios/roles | Todo el dominio del restaurante |
| Claves | Tiene la clave **privada** de firma | Solo consume la clave **pública** vía JWKS |

## Emisión de tokens (Auth API)

- **ASP.NET Core Identity** sobre EF Core / `resto_identity`
  (`AddIdentityCore<AppUser>()` + stores de EF). `AppUser` mínimo; los datos de
  empleado (`employees`, `roles`) viven en `resto_business` y se enlazan por un
  claim (`employee_id` / `branch_id`).
- Al hacer login correctamente se emite:
  - **Access token**: JWT corto, firmado con **clave asimétrica** (RSA 2048 o
    ECDSA P-256). Claims: `sub`, `email`, `role` (uno o varios), y los claims de
    dominio necesarios para autorizar en negocio (`employee_id`, `branch_id`).
    Header con `kid` para permitir rotación.
  - **Refresh token**: opaco, almacenado con hash en `resto_identity`, con
    expiración larga; se canjea en `/auth/refresh`.
- **Publicación de la clave pública**:
  - `GET /.well-known/jwks.json` — JWKS con la(s) clave(s) pública(s) activa(s).
  - `GET /.well-known/openid-configuration` — metadata mínima (issuer,
    `jwks_uri`, algoritmos) para que la Business API se autoconfigure.
- **Gestión de claves**: el par de claves se monta como **secreto en el
  contenedor** (fichero o variable de entorno; ver [`docker.md`](docker.md)), nunca
  en el repo. Soporta más de una clave activa a la vez (`kid`) para rotación sin
  downtime.

## Validación de tokens (Business API)

```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Auth:Authority"];      // http://backend-auth:5001
        options.MetadataAddress = builder.Configuration["Auth:Metadata"]; // .../.well-known/openid-configuration
        options.TokenValidationParameters = new()
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Auth:Audience"],
            ValidateLifetime = true,
            // la clave de firma se resuelve automáticamente vía JWKS (kid)
        };
    });
```

- La Business API **no** llama a la Auth API por cada request: descarga el JWKS y lo
  cachea (refresco periódico). Si un `kid` no está en caché, refresca.
- En desarrollo, dentro de la red de Docker, `Authority` apunta al nombre de
  servicio `backend-auth`.

## Autorización

- **Roles**: los del dominio (`roles` en `resto_business`). El nombre concreto de
  cada rol y **qué puede hacer cada uno** (matriz de permisos) **no está en el
  spec** (`roles` solo tiene `name`, `description`, `hourly_rate`) → se **consulta
  al usuario** antes de implementar cualquier regla de autorización.
- Se usa autorización basada en **policies** (`AddAuthorization(o => o.AddPolicy(...))`)
  más que `[Authorize(Roles="...")]` disperso, para tener las reglas en un solo sitio.
- **`DOM-06`**: el empleado debe estar habilitado para la sucursal sobre la que
  opera. Esto se comprueba en `Application` combinando el claim `branch_id`/
  `employee_id` del token con los datos de `employees` — no basta con el rol.

## Flujo con el frontend

1. `POST /auth/login` → `{ accessToken, refreshToken, expiresIn }`.
2. El frontend guarda los tokens (mecanismo por decidir: `localStorage` vs cookie
   `httpOnly` — **pregunta abierta**, ver abajo) y adjunta
   `Authorization: Bearer <accessToken>` en cada llamada a la Business API mediante
   un **HTTP interceptor** de Angular.
3. Ante un `401`, el interceptor intenta `POST /auth/refresh` una vez; si falla,
   redirige a `signin`.
4. `signin.html` / `signup.html` del template `requirements/inapp/` son el referente
   visual de las pantallas de login/registro.

## Preguntas abiertas (consultar antes de implementar)

- Vida exacta del access token y del refresh token.
- ¿Rotación de refresh tokens (single-use) y detección de reuso?
- ¿Los tokens se guardan en `localStorage` o en cookie `httpOnly` + CSRF?
- ¿Registro público (`/auth/register` abierto) o solo alta de usuarios por un
  administrador?
- Algoritmo de firma concreto (RSA vs ECDSA) y tamaño.
- Catálogo de roles y matriz de permisos por endpoint/módulo.
