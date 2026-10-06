# Guía para desarrollar funcionalidades – Backend Planilla Web

> Parte A: la **receta** que se repite en cualquier módulo (Personal, Vacaciones, Cálculo…).
> Parte B: la receta aplicada al **login** (módulo Acceso), paso a paso.
> Referencias: `ARQUITECTURA_BACKEND.md` (diseño), `INVENTARIO_LOGIN.md` (reglas del VB6).

---

## Estructura del proyecto

```
backend/
  Ejb.Planilla.slnx
  Ejb.Planilla.Api/                 ← el único proyecto de la API
    Program.cs                      Arranque: registra servicios y módulos
    appsettings.json                Configuración (sin contraseñas)
    Compartido/                     Lo que usan todos los módulos
      Resultado.cs                    Resultado<T> y Falla
      Datos/Cn.cs        Conexión a SQL Server
      Seguridad/                      JWT, límite de intentos, claims
      Salud/                          /salud
    Modulos/
      Acceso/                       ← un módulo = una carpeta
        IniciarSesion/              ← una funcionalidad = una carpeta
        CrearContrasenaWeb/
        UsuarioRepo.cs       ← lo que comparten varias funcionalidades del módulo
      Personal/                     (cuando llegue)
  Ejb.Planilla.Pruebas/             ← pruebas, con la misma división por módulo
    Modulos/Acceso/…
```

**Namespaces:** siguen las carpetas. `Modulos/Acceso/IniciarSesion/` → `namespace Ejb.Planilla.Api.Modulos.Acceso.IniciarSesion;` (Visual Studio lo pone solo al crear el archivo dentro de la carpeta).

---

# Parte A – Receta general

Cada funcionalidad (un caso de uso: "iniciar sesión", "registrar trabajador", "calcular planilla") vive en **una carpeta** `Modulos/<Modulo>/<Funcionalidad>/` y sigue siempre los mismos pasos:

| # | Paso | Archivo | Pregunta que responde |
|---|---|---|---|
| 1 | **Entender la regla en el VB6** | — (formulario VB6 + inventario) | ¿Qué hace hoy, con qué tablas y columnas, qué validaciones? ¿Qué NO se migra? |
| 2 | **Definir el contrato** | Postman primero, luego `…Solicitud.cs` / `…Respuesta.cs` | ¿Qué envía el frontend y qué recibe? ¿Qué códigos HTTP? |
| 3 | **Datos** | `…Repositorio.cs` (en la carpeta del módulo si lo comparten varias funcionalidades) | ¿Qué SQL exacto se ejecuta? |
| 4 | **Validador** | `…Validador.cs` | ¿Qué datos de entrada son inválidos antes de tocar la base? |
| 5 | **Servicio** (la lógica) | `…Servicio.cs` | ¿En qué orden se hacen las cosas y qué resultado sale? |
| 6 | **Controlador** | `…Controller.cs` | ¿Ruta, verbo, quién puede usarla, cómo se traduce el resultado a HTTP? |
| 7 | **Registro** | `Program.cs` | Dar de alta las clases nuevas (`builder.Services.AddScoped<…>()`) |
| 8 | **Pruebas** | `Ejb.Planilla.Pruebas/Modulos/<Modulo>/` | ¿Se cumplen las reglas, incluidos los casos de error? |
| 9 | **Prueba manual en Postman** | Colección del módulo | ¿Funciona contra `EJBBENNY`? |
| 10 | **Revisar logs y commit** | `logs/` + Git | ¿Se registra lo necesario y nada sensible? |

**El controlador es delgado:** recibe, valida, llama al servicio y responde. **La lógica va en el servicio.**

### Reglas que aplican siempre

- **SQL:** siempre con parámetros (`@codigo`), columnas explícitas (nunca `SELECT *`), `RTRIM()` en las columnas `char`.
- **Parámetros `char`:** Dapper envía los textos como `nvarchar`; para columnas `char`/`varchar` usar `new DbString { Value = x, IsAnsi = true, Length = n }`. Si no, SQL Server convierte la columna y no usa el índice.
- **Montos:** `decimal` y redondeo con `Math.Round(v, 2, MidpointRounding.AwayFromZero)` (D2).
- **Errores de negocio:** el servicio **no lanza excepciones**; devuelve `Resultado<T>.Fallo(...)` y el controlador lo traduce a HTTP.
- **Sesión:** todo endpoint exige token; solo los públicos (como el login) llevan `[AllowAnonymous]`.
- **Cambios de base:** nunca desde el código. Script en `db/cambios/`, aprobación (D6) y agregarlo a la lista de `Compartido/Salud/ChequeosSalud.cs`.
- **Algo nuevo en `Compartido/` solo cuando lo necesiten dos módulos.** Si solo lo usa uno, va en la carpeta de ese módulo.

### Conceptos de C# que se usan en el proyecto

**`class` vs `record`**

| | `class` | `record` |
|---|---|---|
| Para qué | Objetos que **hacen algo** (tienen métodos con lógica) | Objetos que **solo llevan datos** |
| Ejemplos | `HashContrasena`, `EmisorTokens`, servicios, repositorios, controladores | `Falla`, `TokenEmitido`, solicitudes, respuestas, `UsuarioAcceso` |
| Forma corta `Nombre(tipo A, tipo B);` | Los valores son **solo parámetros del constructor**: no se pueden leer desde fuera | Crea **propiedades públicas** `A` y `B` de forma automática, de solo lectura |
| Comparar dos objetos con `==` | Son iguales solo si son **el mismo objeto** en memoria | Son iguales si tienen **los mismos datos** |

Regla práctica: **si solo transporta datos → `record`; si tiene lógica → `class`.**

**`sealed`**

Significa que **nadie puede heredar** de esa clase o record (`class Otra : HashContrasena` daría error). Se pone por defecto en todo el proyecto porque:
- deja claro que la clase no está pensada para extenderse;
- evita que alguien cambie su comportamiento heredando de ella;
- permite al compilador optimizar un poco.

Si algún día de verdad hace falta heredar, se quita el `sealed` en ese caso concreto.

**Otros que aparecen**

| Palabra | Significado |
|---|---|
| `public` / `private` | Visible desde cualquier parte / solo dentro de la clase |
| `readonly` (en un campo) | Se asigna una vez (al declararlo o en el constructor) y no cambia |
| `{ get; }` (en una propiedad) | Se puede leer desde fuera, pero no modificar |
| `static` | Pertenece a la clase, no a un objeto: se usa sin `new` (`Resultado<T>.Ok(...)`) |
| `static class` | Clase que solo guarda valores o métodos `static`; nunca se crea con `new` y no lleva `sealed` (ej. `ErroresAcceso`) |
| `const` | Valor fijo **simple** (número, texto), decidido al compilar (ej. `OpcionesJwt.Seccion`) |
| `static readonly` | Valor fijo que es un **objeto** (`const` no lo admite); se crea una vez al iniciar (ej. `ErroresAcceso.CredencialesInvalidas`) |
| `;` vs `{ }` al declarar | Un `record` de una línea termina en `;`. Una clase con contenido lleva `{ }`, y todo campo o método va **dentro** de ellas |
| `T?` | Puede estar vacío (`null`) |
| `=>` en un método | Forma corta de un método que solo devuelve una expresión |
| `IOptions<T>` en un constructor | .NET entrega la sección de configuración correspondiente (inyección de dependencias) |

### Nombres de variables y parámetros

**Decisión del responsable técnico:** cuando un dato ya tiene nombre en el VB6, se **conserva el nombre del VB6**, porque es el que conoce el equipo. Usar el nombre **exacto** del formulario de origen:

| Dato | Nombre (VB6 → C#) | Origen |
|---|---|---|
| Código de usuario | `wsCodUsu` | `GENPAS01.frm` |
| Contraseña VB6 (`US1_CPASSWO`) | `wsPasUsu` | `GENPAS01.frm` |
| Tipo de usuario (`US1_CTIPUSU`) | `wsTipUsu` | `GENPAS01.frm` |
| Código de compañía | `wsCodCom` | global del VB6 |
| Conexión a la base | `Cn` / `ICn` (`using var cn = _cn.Principal();`) | global `Cn` del VB6 |

**Propiedades de records (opción B):** el nombre del VB6 **sin** el prefijo `ws` y en PascalCase. Ej. `UsuarioAcceso`: `CodUsu`, `NomUsu`, `TipUsu`, `PasUsu`. Para datos sin variable en el VB6, un nombre en el mismo estilo: `EstadoUsu` (`US1_CESTADO`), `PasWbh` (`US1_CPASWBH`). En el SQL, el `AS` debe ser igual al nombre de la propiedad para que Dapper la llene.

- ⚠️ `wsEstUsu` en el VB6 guarda `US1_CCODEST` (**estructura organizativa**, no el estado). El estado (`US1_CESTADO`) no tiene variable en el VB6.
- Datos nuevos que no existen en el VB6 (hash web, tokens…) llevan nombre descriptivo en español y camelCase: `hash`, `tokenAcceso`.
- Clases, métodos y propiedades: PascalCase en español (`HashContrasena`, `Verificar`).

### ¿Y las variables globales del VB6?

En .NET **no se declaran variables globales**. Cada cosa que el VB6 guardaba en un global tiene su reemplazo, y se recibe por el **constructor** de la clase que la necesita (inyección de dependencias):

| En el VB6 | En el backend | Cómo se usa |
|---|---|---|
| `Cn` (conexión abierta global) | `ICn` | `using var cn = _cn.Principal();` dentro de cada método del repositorio |
| `wsCodUsu`, `wsCodCom`, `wsCodSuc`, periodo | Datos (claims) del **token** de la petición | `User.WsCodUsu()`, `User.WsCodCom()`… (se crean en B6, `Compartido/Seguridad/`) |
| Nombres de tabla por compañía (`PLAPER1`…) | `Tabla.Cia("PLAPER1", wsCodCom)` → `[PLA0001PER1]`, misma regla del VB6 y de `Workspace::table()` de WEB_EJBCONTABLE | Se crea en B6 (`Compartido/Datos/Tabla.cs`) |
| Parámetros `VP*` | Se leen de `GEN{cía}TGE1` cuando un módulo los necesite | Se definirá con el primer módulo que los use |
| Datos de `ejb.com` (servidor, base, usuario) | Configuración: `appsettings.json` + User Secrets / `web.config` | `IOptions<OpcionesJwt>` o `IConfiguration` |
| Constantes repetidas | `const` en una clase `static` | ej. `ErroresAcceso.CredencialesInvalidas` |

> Regla práctica: si una clase necesita algo, lo **pide en su constructor**. Nunca se guardan datos de un usuario en un campo `static`: la API atiende a muchos usuarios a la vez y se mezclarían.

---

# Parte B – El login, paso a paso

## Archivos a crear y modificar

"Miembros" indica **qué debe tener**, no el código.

**Ya hechos**

| Archivo | Estado |
|---|---|
| `Compartido/Resultado.cs` (`Resultado<T>`, `Falla`) | ✅ B1.1 |
| `Compartido/Seguridad/OpcionesJwt.cs` (con las 3 audiencias) | ✅ B1.2 |
| `appsettings.json` → sección `Jwt` con `AudienciaRenovacion`, `AudienciaContrasena`, `MinutosContrasena` | ✅ |
| `Compartido/Seguridad/ConfiguracionSeguridad.cs` → JWT + límite `LimiteLogin` | ✅ |

**Módulo Acceso** (`Ejb.Planilla.Api/Modulos/Acceso/`)

| Paso | Archivo | Tipo | Miembros |
|---|---|---|---|
| ✅ B1.3 | `HashContrasena.cs` | clase | `string Calcular(string wsCodUsu, string wsPasUsu)`; `PasswordVerificationResult Verificar(string wsCodUsu, string hash, string wsPasUsu)` (usa `PasswordHasher<string>` de `Microsoft.AspNetCore.Identity`, ya incluido en ASP.NET Core) |
| ✅ B1.4 | `EmisorTokens.cs` | clase + `record TokenEmitido(string Token, DateTimeOffset Expira)` | Constructor: `IOptions<OpcionesJwt>`. `Acceso(wsCodUsu, nombreUsuario)` (15 min), `Renovacion(wsCodUsu, sello)` (10 h), `CambioContrasena(wsCodUsu)` (5 min), `static Sello(hashWeb, estado)` |
| ✅ B2 | `ErroresAcceso.cs` | `static class` | `static readonly Falla CredencialesInvalidas` |
| ✅ B2 | `UsuarioAcceso.cs` | `sealed record` | `CodUsu`, `NomUsu`, `TipUsu`, `EstadoUsu`, `PasUsu`, `PasWbh` (string?) — nombres del VB6 (opción B); propiedades calculadas `EstaActivo`, `EsMaestro`, `DebeCrearContrasena` |
| ✅ B2 | `IUsuarioRepo.cs` + `UsuarioRepo.cs` | interfaz + clase | `Task<UsuarioAcceso?> Buscar(string wsCodUsu, CancellationToken ct)`; `Task GuardarHashWeb(string wsCodUsu, string hash, CancellationToken ct)` (un solo archivo `UsuarioRepo.cs`) |
| ✅ B2 | `IniciarSesion/IniciarSesionSolicitud.cs` | `sealed record` | `Usuario`, `Contrasena` |
| ✅ B2 | `IniciarSesion/IniciarSesionRespuesta.cs` | `sealed record` | `TokenAcceso?`, `ExpiraEn?`, `Usuario?`, `DebeCrearContrasena`, `TokenContrasena?` + token de renovación con `[JsonIgnore]` (va en cookie, no en el JSON) |
| ✅ B2 | `IniciarSesion/IniciarSesionValidador.cs` | `AbstractValidator<IniciarSesionSolicitud>` | Reglas del paso B2.4 |
| ✅ B2 | `IniciarSesion/IniciarSesionServicio.cs` | clase | Constructor: `IUsuarioRepo`, `HashContrasena`, `EmisorTokens`, `ILogger<IniciarSesionServicio>`. Método `Task<Resultado<IniciarSesionRespuesta>> Ejecutar(IniciarSesionSolicitud s, CancellationToken ct)` |
| ✅ B2 | `IniciarSesion/IniciarSesionController.cs` | `ControllerBase` | Acción `Post` |
| ✅ B3 | `CrearContrasenaWeb/` (Solicitud, Validador, Servicio, Controller) + `CookieRenovacion.cs` (cookie compartida con IniciarSesion) | igual que IniciarSesion | Solicitud: `Nueva`, `Confirmacion`. El usuario sale del token, **no** del cuerpo |

**Otros**

| Paso | Archivo | Qué |
|---|---|---|
| B2/B3 | `Program.cs` | Registrar `UsuarioRepo`, servicios (`AddScoped`), `HashContrasena`, `EmisorTokens` (`AddSingleton`) y los validadores (`AddValidatorsFromAssemblyContaining<Program>()`) |
| ✅ B3 | `Compartido/Seguridad/ConfiguracionSeguridad.cs` | Agregado un segundo esquema JwtBearer `"Contrasena"` con `AudienciaContrasena` |
| B2 | `Ejb.Planilla.Pruebas/Modulos/Acceso/` | `UsuarioRepoFalso.cs` + pruebas de los servicios y validadores |

No hacen falta paquetes NuGet nuevos: `PasswordHasher` viene con ASP.NET Core y `JsonWebTokenHandler` llega con el paquete JwtBearer que ya tiene el proyecto.

---

## Funcionalidades del login

| Orden | Funcionalidad | Ruta |
|---|---|---|
| **B1** | Piezas comunes (resultado, opciones JWT, hash, emisor de tokens) | — |
| ✅ **B2** | **IniciarSesion** | `POST /api/acceso/login` |
| ✅ **B3** | **CrearContrasenaWeb** (primer ingreso) | `POST /api/acceso/contrasena` |
| B4 | RenovarSesion | `POST /api/acceso/renovar` |
| B5 | CerrarSesion | `POST /api/acceso/logout` |
| B6 | ElegirContexto: **fase 2 del login** (compañía, sucursal, tipo de planilla, periodo). Ver `ARQUITECTURA_BACKEND.md` §8.1 | `GET /api/acceso/contexto/opciones`, `POST /api/acceso/contexto` |
| B7 | ObtenerMenu (permisos de `GENUSU2`) | `GET /api/acceso/menu` |

## Paso 0 – Requisitos

- [ ] Ejecutar `db/cambios/001_GENUSU1_US1_CPASWBH.sql` en `EJBBENNY`.
- [ ] `GET /salud` en Postman → **200** (los dos chequeos `Healthy`).

## Paso 1 – Entender la regla en el VB6

Leer `GENPAS01.frm` (proPLANILLA) y `INVENTARIO_LOGIN.md` §4 y §5. Lo que importa para B2/B3:

| Dato | Columna | Notas |
|---|---|---|
| Código de usuario | `US1_CCODUSU` `char(5)` (clave primaria) | |
| Nombre | `US1_CNOMUSU` `varchar(40)` | |
| Contraseña VB6 | `US1_CPASSWO` `char(8)`, **texto plano** | Solo se lee para el primer ingreso; **nunca** se devuelve ni se registra |
| Contraseña web | `US1_CPASWBH` `varchar(256)` (hash) | Vacía hasta el primer ingreso |
| Estado | `US1_CESTADO` `char(1)` | `'1'` activo, `'0'` inactivo |
| Tipo | `US1_CTIPUSU` `char(1)` | `'M'` maestro (ve todas las compañías), `'O'` otro |

**Decisiones del login**

| # | Punto | Decisión |
|---|---|---|
| 1 | Usuarios con `US1_CESTADO = '0'` (el VB6 no los bloquea) | ✅ La web **sí los bloquea** |
| 2 | ¿La contraseña VB6 distingue mayúsculas/minúsculas? | ✅ **Sí**: comparación exacta |
| 3 | Reglas de la contraseña web nueva | ✅ **Ninguna por ahora** (solo no vacía y confirmación igual) |
| 4 | Bloqueo de la cuenta tras intentos fallidos | ✅ **No por ahora**. Solo el límite de 5 peticiones por minuto por IP en `/login` (responde 429), que no bloquea la cuenta |

> ℹ️ `US1_CCODEST` **no es el estado**: es el código de estructura organizativa (`PLAEST1`, `'T'` = todas). Se usará en B6.

**Lo que NO se migra:** contraseña maestra fija, comparación en texto plano (salvo el primer ingreso), mensajes que distinguen "usuario no existe" de "contraseña incorrecta", `VPBAJLOG`.

## Paso 2 – Definir el contrato en Postman

Crear en Postman la colección **"Planilla – Acceso"** con un *Environment* "Local": `base` = `https://localhost:7180`, `token` y `tokenContrasena` vacíos.

`POST {{base}}/api/acceso/login`:

```json
// Petición
{ "usuario": "ADMIN", "contrasena": "••••••" }

// 200 – ingreso correcto
{ "tokenAcceso": "eyJ…", "expiraEn": 900, "usuario": { "codigo": "ADMIN", "nombre": "…", "esMaestro": true } }

// 200 – primer ingreso (aún no tiene contraseña web)
{ "debeCrearContrasena": true, "tokenContrasena": "eyJ…", "expiraEn": 300 }

// 401 – cualquier fallo (mismo mensaje siempre)
{ "title": "Usuario o contraseña incorrectos", "status": 401 }

// 400 – datos vacíos o demasiado largos
// 429 – demasiados intentos
```

## B1.3 – Hash de contraseñas

`Modulos/Acceso/HashContrasena.cs`: envuelve `PasswordHasher<string>` (el `string` es el código de usuario; el hasher no lo usa para calcular, solo lo exige su firma).
- `Calcular` → texto que se guarda en `US1_CPASWBH` (PBKDF2, con sal y versión incluidas).
- `Verificar` → `Failed`, `Success` o `SuccessRehashNeeded` (en este último caso, volver a calcular y guardar el hash).

## B1.4 – Emisor de tokens

`Modulos/Acceso/EmisorTokens.cs`: recibe `IOptions<OpcionesJwt>` y crea los tokens con `JsonWebTokenHandler` + `SecurityTokenDescriptor`:

| Token | Duración | Audiencia | Claims |
|---|---|---|---|
| Acceso | `MinutosAcceso` (15) | `Audiencia` | `sub`, `name` (en B6 se agregan compañía, sucursal, tipo de planilla, periodo) |
| Renovación | `HorasRenovacion` (10) | `AudienciaRenovacion` | `sub`, `sello` |
| Cambio de contraseña | `MinutosContrasena` (5) | `AudienciaContrasena` | `sub` |

> Con **audiencias distintas**, la API rechaza sola un token de renovación o de contraseña enviado como token de acceso.

**Sello de seguridad:** un resumen SHA-256 corto de `US1_CPASWBH + US1_CESTADO`. Se recalcula al renovar (B4): si el usuario cambió su contraseña o fue desactivado, deja de coincidir.

## B2.3 – Datos

`Modulos/Acceso/UsuarioRepo.cs` (con su interfaz `IUsuarioRepo`), usando `ICn` (la `Cn` del VB6):

| Método | SQL (idea) |
|---|---|
| `Buscar(wsCodUsu)` | `SELECT RTRIM(US1_CCODUSU) AS CodUsu, RTRIM(US1_CNOMUSU) AS NomUsu, US1_CTIPUSU AS TipUsu, US1_CESTADO AS EstadoUsu, RTRIM(US1_CPASSWO) AS PasUsu, US1_CPASWBH AS PasWbh FROM GENUSU1 WHERE US1_CCODUSU = @wsCodUsu` |
| `GuardarHashWeb(codigo, hash)` | `UPDATE GENUSU1 SET US1_CPASWBH = @hash WHERE US1_CCODUSU = @codigo` |

- `@codigo` como `DbString { IsAnsi = true, Length = 5 }`.
- Métodos asíncronos de Dapper (`QuerySingleOrDefaultAsync`, `ExecuteAsync`) con `using var cn = _cn.Principal();`.
- La **interfaz** existe para poder probar el servicio con un repositorio falso, sin base de datos.

## B2.4 – Validador

`IniciarSesionValidador : AbstractValidator<IniciarSesionSolicitud>`:
- `usuario`: obligatorio, máximo 5 caracteres.
- `contrasena`: obligatoria, máximo 128 caracteres.

## B2.5 – Servicio (la lógica)

`IniciarSesionServicio.Ejecutar(solicitud)` → `Resultado<IniciarSesionRespuesta>`. Orden:

1. **Buscar** el usuario por código (en mayúsculas y sin espacios).
2. **No existe** → ejecutar igualmente una verificación de hash "falsa" (para que tarde lo mismo y no se pueda adivinar qué usuarios existen) → `Fallo(ErroresAcceso.CredencialesInvalidas)`.
3. **Inactivo** (`US1_CESTADO <> '1'`) → `CredencialesInvalidas`. En el log sí se anota el motivo real.
4. **Sin contraseña web** (primer ingreso):
   - Comparar con `US1_CPASSWO` **distinguiendo mayúsculas**, con `CryptographicOperations.FixedTimeEquals` sobre los bytes UTF-8.
   - Correcta → `debeCrearContrasena: true` + token de cambio de contraseña. **Sin** token de acceso.
   - Incorrecta → `CredencialesInvalidas`.
5. **Con contraseña web** → `Verificar`: `Failed` → `CredencialesInvalidas`; `SuccessRehashNeeded` → recalcular y guardar.
6. **Éxito** → token de acceso + token de renovación → `Ok(respuesta)`.
7. **Log:** código de usuario y resultado. **Nunca** la contraseña ni los tokens.

## B2.6 – Controlador

`IniciarSesion/IniciarSesionController.cs`:
- `[ApiController]`, `[Route("api/acceso/login")]`, `[AllowAnonymous]`, `[EnableRateLimiting(ConfiguracionSeguridad.LimiteLogin)]`, acción `[HttpPost]`.
- Valida (400 si falla), llama al servicio y traduce: éxito → `200`; error → `Problem(title: …, statusCode: 401)`.
- El token de renovación va en una cookie: `Response.Cookies.Append("planilla_renovar", token, new CookieOptions { HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict, Path = "/api/acceso/renovar", Expires = … })`.

## B2.7 – Registro en `Program.cs`

Debajo del comentario `// Módulos`:
- `AddScoped<IUsuarioRepo, UsuarioRepo>()` y `AddScoped<IniciarSesionServicio>()`.
- `AddSingleton<HashContrasena>()` y `AddSingleton<EmisorTokens>()`.
- `AddValidatorsFromAssemblyContaining<Program>()` (una sola vez: registra todos los validadores).

## B2.8 – Pruebas

En `Ejb.Planilla.Pruebas/Modulos/Acceso/`, con un `UsuarioRepoFalso` (lista en memoria):

| Caso | Resultado esperado |
|---|---|
| Usuario no existe | `CredencialesInvalidas` |
| Contraseña web incorrecta | `CredencialesInvalidas` |
| Usuario inactivo con contraseña correcta | `CredencialesInvalidas` |
| Primer ingreso, contraseña VB6 correcta | `debeCrearContrasena` + token de contraseña, **sin** token de acceso |
| Primer ingreso, contraseña VB6 con otra mayúscula | `CredencialesInvalidas` |
| Contraseña web correcta | Token de acceso con `sub` = código de usuario |
| Validador: usuario vacío / de 6 caracteres | Inválido |

`dotnet test --solution Ejb.Planilla.slnx` → todo en verde.

## B2.9 – Postman

| Petición | Esperado |
|---|---|
| Usuario inexistente | 401, mensaje genérico |
| Contraseña incorrecta | 401, **mismo** mensaje |
| Cuerpo vacío | 400 con la lista de campos |
| 6 intentos en un minuto | El 6.º → 429 |
| Usuario sin contraseña web | 200 con `debeCrearContrasena` |
| Login correcto (después de B3) | 200 con `tokenAcceso`; cookie `planilla_renovar` en la pestaña *Cookies* |

Guardar el token automáticamente (pestaña **Scripts → Post-response** del login):
`pm.environment.set("token", pm.response.json().tokenAcceso);`
y en las peticiones protegidas: **Authorization → Bearer Token → `{{token}}`**.

## B2.10 – Logs y commit

- Revisar `Ejb.Planilla.Api/logs/planilla-AAAAMMDD.log`: intentos con usuario y resultado, **sin** contraseñas ni tokens.
- `git add` + `git commit -m "Acceso: iniciar sesión"`.

## B3 – CrearContrasenaWeb (primer ingreso)

Misma receta. Diferencias:
- **Autorización:** solo acepta el token de cambio de contraseña. En `ConfiguracionSeguridad` se agrega un segundo esquema JwtBearer `"Contrasena"` con `AudienciaContrasena`, y el controlador usa `[Authorize(AuthenticationSchemes = "Contrasena")]`.
- **Solicitud:** `nueva` y `confirmacion`. Sin reglas de complejidad (decisión 3): no vacía, máximo 128, confirmación igual.
- **Servicio:** `Calcular` hash → `GuardarHashWeb` → token de acceso + renovación (el usuario queda dentro).
- **Pruebas:** confirmación distinta, contraseña vacía, token de acceso en lugar del de contraseña (401), token vencido.

## Después: B4 a B7

Misma receta. Detalles en `ARQUITECTURA_BACKEND.md` §8 (renovación, sello, cookie) e `INVENTARIO_LOGIN.md` §5–§7 (compañías por `SEGAXU1`, usuario maestro, sucursal, tipo de planilla, periodo, permisos de `GENUSU2`).
