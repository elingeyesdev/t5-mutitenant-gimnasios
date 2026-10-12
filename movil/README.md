# movil/ · App móvil

.NET MAUI 10 para Android. Consume la API de `web/`; nunca se conecta directo a la base de datos.
Las pestañas cambian según el rol de quien inicia sesión (cliente, entrenador, instructor,
recepción, gerente); ver [docs/migracion.md](../docs/migracion.md#pantallas-de-la-app-por-rol).

## Estructura

```
GestionGimnasios.Movil/
├── Servicios/   # Api (cliente HTTP con el token) y Sesion (token en SecureStorage)
├── Modelos/     # Contratos de la API y textos/colores para mostrarlos
├── Paginas/     # PaginaBase y pantallas: Comun/, Cliente/, Personal/
├── AppShell.cs  # Pestañas según el rol
└── Resources/   # Colors.xaml, Styles.xaml, Propios.xaml, íconos
```

## Servidor de la API

Primero levantar la web (`web/README.md`). La app usa por defecto `http://10.0.2.2:5253`, que en el
emulador de Android apunta a la computadora. En un celular físico conectado a la misma red Wi-Fi,
tocar "Servidor" en la pantalla de inicio de sesión y escribir la IP de la computadora
(por ejemplo `http://192.168.1.20:5253`); la web debe escuchar en esa IP:

```bash
dotnet run --project ../web/GestionGimnasios.Web --urls http://0.0.0.0:5253
```

## Ejecutar en el emulador

```bash
cd GestionGimnasios.Movil
dotnet build -t:Run -f net10.0-android
```

Para probar rápido sin emulador (solo en Windows):

```bash
dotnet build -t:Run -f net10.0-windows10.0.19041.0
```

Si la compilación de Android no encuentra Java, indicar el JDK 17+ explícitamente:

```bash
dotnet build -f net10.0-android -p:JavaSdkDirectory="C:\Program Files\Java\jdk-21.0.11"
```

## Generar el APK release

```bash
cd GestionGimnasios.Movil
dotnet publish -f net10.0-android -c Release -p:AndroidPackageFormats=apk
```

Copiar el `*-Signed.apk` de `bin/Release/net10.0-android/publish/` a
`movil/releases/app-v0.N.0-release.apk` con la versión del tag.

- Siempre release, nunca debug.
- GitHub rechaza archivos de más de 100 MB.
