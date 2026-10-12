# Gestión Gimnasios

Plataforma multi-tenant para administrar los gimnasios de Santa Cruz de la Sierra: cada cadena
(por ejemplo, una marca con varias sucursales) es un inquilino independiente que gestiona sus
sedes, socios, entrenadores, rutinas, planes de alimentación y seguimiento.

Estado: **migración del proyecto base (GymSync) al Stack B completa** (web, API y app Android
probadas con los datos del proyecto base; ver [docs/migracion.md](docs/migracion.md)). Las mejoras
propuestas en el análisis de soluciones (RF-01 a RF-06) se trabajan sobre esta base.

## Stack (Stack B · C# de punta a punta)

| Producto | Carpeta | Tecnología |
|---|---|---|
| Web (panel + API REST) | `web/` | ASP.NET Core 10 MVC/Razor + Web API |
| App móvil | `movil/` | .NET MAUI 10 (Android), consume la API de la web |
| Base de datos | — | SQL Server con migraciones de EF Core |

La app móvil nunca se conecta directo a la base de datos.

## Estructura

```
gestion-gimnasios/
├── web/                 # Aplicación web y API
│   ├── GestionGimnasios.Web/
│   └── GestionGimnasios.Web.Tests/
├── movil/               # App móvil
│   ├── GestionGimnasios.Movil/
│   └── releases/        # APK release de cada versión
├── docs/                # Contexto, diagnóstico del proyecto base, diseño
├── .github/             # CI, plantillas de issues y Pull Requests
├── dotnet-tools.json    # dotnet-ef fijado para todo el equipo
└── global.json          # versión del SDK de .NET
```

## Requisitos

- .NET SDK 10.0.301 o superior de la banda 10.0.3xx
- Workload de MAUI para Android (`dotnet workload install maui-android`)
- SQL Server 2019+ (Developer o Express) en `localhost`
- JDK 17+ y Android SDK (para compilar `movil/`)

## Primeros pasos

```bash
git clone https://github.com/elingeyesdev/t5-mutitenant-gimnasios.git
cd t5-mutitenant-gimnasios
dotnet tool restore          # instala dotnet-ef 10.0.12 del manifiesto
```

Luego seguir el README de cada producto: [web/README.md](web/README.md) y
[movil/README.md](movil/README.md).

## Forma de trabajo

- Ramas: `main` (producción, protegida), `develop` (integración, rama por defecto),
  `feature/N-descripcion`, `fix/N-...`, `hotfix/N-...`.
- Commits con Conventional Commits y alcance por producto: `feat(web): ...`, `fix(movil): ...`.
- Todo cambio entra por Pull Request hacia `develop` con `Closes #N`, aprobado por el
  compañero y con CI en verde.
- Cada viernes: PR `develop` → `main`, tag `v0.N.0` y GitHub Release.

## Documentación

- [docs/migracion.md](docs/migracion.md): qué se migró, cuentas de prueba, tecnologías pendientes de aprobación y pruebas.
- [docs/contexto-proyecto.md](docs/contexto-proyecto.md): qué pidió el docente, requerimientos y decisiones.
- [docs/diagnostico-proyecto-base.md](docs/diagnostico-proyecto-base.md): análisis y ejecución del proyecto base a migrar.
- [docs/planificacion/](docs/planificacion/README.md): EDT, PERT, CPM, iteraciones y Planner.
- [docs/diseno/casos-de-uso.md](docs/diseno/casos-de-uso.md) y
  [docs/diseno/historias-de-usuario.md](docs/diseno/historias-de-usuario.md): requisitos de la migración.
