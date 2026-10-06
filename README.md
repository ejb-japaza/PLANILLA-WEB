# Planilla Web – EJB

Migración del sistema de planillas VB6 a web: backend **.NET 10** + frontend **Vue**.
Diseño: `../migracion-web/ARQUITECTURA_BACKEND.md`. Cómo desarrollar: `docs/GUIA_DESARROLLO_FUNCIONALIDADES.md`.

## Estructura

```
backend/
  Ejb.Planilla.slnx          Solución – se abre con Visual Studio 2026
  Ejb.Planilla.Api/          La API (un solo proyecto)
    Compartido/              Lo que usan todos los módulos
    Modulos/<Modulo>/        Un módulo por carpeta; dentro, una carpeta por funcionalidad
  Ejb.Planilla.Pruebas/      Pruebas
frontend/                    Vue – se abre con VS Code (pendiente)
db/cambios/                  Scripts SQL aprobados (NNN_*.sql + reversión)
docs/                        Documentación
```

## Primer uso en un equipo de desarrollo

1. Ejecutar `db/cambios/001_GENUSU1_US1_CPASWBH.sql` en la base (desarrollo: `EJBBENNY`).
2. Configurar los secretos locales (no se suben a Git). En Visual Studio: clic derecho en `Ejb.Planilla.Api` → **Administrar secretos de usuario**:

   ```json
   {
     "ConnectionStrings:Principal": "Server=.;Database=EJBBENNY;Integrated Security=True;TrustServerCertificate=True",
     "Jwt:ClaveFirma": "<clave aleatoria de al menos 32 caracteres>"
   }
   ```
3. Opcional: `dotnet dev-certs https --trust` para que Postman y el navegador acepten el certificado local.

## Comandos (desde `backend/`)

| Acción | Comando |
|---|---|
| Compilar | `dotnet build Ejb.Planilla.slnx` |
| Pruebas | `dotnet test --solution Ejb.Planilla.slnx` |
| Ejecutar la API | `dotnet run --project Ejb.Planilla.Api` → `https://localhost:7180` (probar con Postman) |

## Rutas de verificación

| Ruta | Qué comprueba |
|---|---|
| `GET /salud` | Conexión a la base (`base-datos`) y cambios de BD aprobados (`cambios-bd`). 200 = todo bien, 503 = algo falta (el detalle lo indica) |
| `GET /openapi/v1.json` | Especificación de la API (solo en desarrollo): en Postman, **Import** → pegar la URL |

## Agregar un módulo

1. Crear la carpeta `Ejb.Planilla.Api/Modulos/<Modulo>/` y dentro una carpeta por funcionalidad.
2. Registrar sus clases en `Program.cs`, debajo de `// Módulos`.
3. Sus pruebas en `Ejb.Planilla.Pruebas/Modulos/<Modulo>/`.
