# web/ · Aplicación web y API

ASP.NET Core 10 (MVC/Razor para el panel de administración + Web API para la app móvil),
EF Core 10 con SQL Server.

## Configuración local

1. Copiar la configuración de ejemplo (el archivo real no se sube al repo):

   ```bash
   cp GestionGimnasios.Web/appsettings.Development.example.json GestionGimnasios.Web/appsettings.Development.json
   ```

2. Ajustar `ConnectionStrings:Default` si el SQL Server no está en `localhost`
   (por ejemplo `Server=localhost\\SQLEXPRESS;...`).

3. Crear o actualizar la base de datos con las migraciones:

   ```bash
   cd GestionGimnasios.Web
   dotnet ef database update
   ```

   Al arrancar, la web también aplica las migraciones pendientes y, si `BaseDatos:CargarDatosDePrueba`
   es `true` y la base está vacía, carga los datos de prueba del proyecto base (10 cadenas,
   30 sucursales, 206 usuarios). Las cuentas están en [docs/migracion.md](../docs/migracion.md#cuentas-de-prueba).

## Ejecutar

```bash
dotnet run --project GestionGimnasios.Web --launch-profile http
```

- Panel: http://localhost:5253 (super administrador: `admin@gymsync.com` / `Aaron123*`)
- API para la app: http://localhost:5253/api (documentación OpenAPI en `/openapi/v1.json`)
- Verificación del entorno: http://localhost:5253/api/salud → `{"estado":"ok","baseDeDatos":true}`

Para recuperar contraseña sin servidor de correo configurado (`Correo` en appsettings), el código
de 6 dígitos se escribe en la consola de la web.

## Pruebas

```bash
dotnet test GestionGimnasios.Web.slnx
```

## Migraciones de EF Core

```bash
cd GestionGimnasios.Web
dotnet ef migrations add NombreDeLaMigracion
dotnet ef database update
```

Las migraciones se suben al repo: la base de datos se recrea en minutos en cualquier equipo.
