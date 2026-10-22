# Módulo Acceso

Login en dos fases, contraseña web y sesión (JWT). Una carpeta por funcionalidad:

```
Acceso/
  IniciarSesion/        Fase 1: POST /api/acceso/login
  CrearContrasenaWeb/   Primer ingreso: POST /api/acceso/contrasena
  ElegirContexto/       Fase 2: GET /api/acceso/contexto/opciones, POST /api/acceso/contexto,
                        GET /api/acceso/contexto (contexto actual, exige ConContexto)
  UsuarioRepo.cs        SQL de GENUSU1
  ContextoRepo.cs       SQL de GENPAS01 (compañías, sucursales, VPQUINCE) y fExtPerAct (periodo)
  EmisorTokens.cs, HashContrasena.cs, CookieRenovacion.cs, ErroresAcceso.cs, UsuarioAcceso.cs
```

Guía paso a paso: `docs/GUIA_DESARROLLO_FUNCIONALIDADES.md`.
