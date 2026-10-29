# Controles de consentimiento del perfil deportivo en móvil

## Identificación

- **Issue:** #55
- **Requisito funcional:** RF-02 · Portabilidad del perfil deportivo con consentimiento explícito.
- **Historia de usuario:** HU-47 · Perfil deportivo portable con consentimiento.
- **Caso de uso:** CU-36 · Autorizar la portabilidad del perfil.
- **EDT:** Actividad F · 3.2 · RF-02: controles de consentimiento en móvil.

## Objetivo y navegación

La pantalla permite que el socio decida qué datos de su perfil deportivo puede consultar otra
cadena de gimnasios. La autorización queda registrada con su alcance, fecha y estado, y puede ser
revocada posteriormente.

El acceso está disponible para socios en:

**Más → Autorizaciones de perfil**

La opción se agrega desde `movil/GestionGimnasios.Movil/Paginas/Comun/MasPage.cs` y abre la pantalla
`ConsentimientoPage`.

## Funcionalidades implementadas

La pantalla `ConsentimientoPage` permite:

- consultar las cadenas disponibles para autorizar;
- seleccionar los datos que se compartirán: `PERFIL`, `RESTRICCIONES`, `HISTORIAL` y `METRICAS`;
- registrar una autorización para la cadena seleccionada;
- consultar autorizaciones vigentes y revocadas, con sus datos y fechas;
- revocar una autorización vigente después de confirmar la acción;
- consultar el historial de accesos de cada autorización, indicando quién consultó el perfil y
  cuándo;
- mostrar mensajes cuando no hay cadenas disponibles o no existen autorizaciones;
- actualizar la información al volver a mostrar la pantalla o mediante el gesto de refresco.

Los errores de la API se muestran mediante el manejo común de `PaginaBase` y el mensaje devuelto
por el servidor.

## Integración con el backend

La pantalla utiliza `Sesion.Api`, que envía las solicitudes autenticadas con el token JWT de la
sesión. Las rutas pertenecen a `api/portabilidad`.

| Método y ruta | Solicitud | Respuesta |
|---|---|---|
| `GET /api/portabilidad/cadenas` | Sin cuerpo | Lista de cadenas con `Id`, `Nombre` y `Descripcion` nullable. |
| `GET /api/portabilidad/consentimientos` | Sin cuerpo | Lista de consentimientos con `Id`, `UsuarioId`, `Socio`, `CadenaId`, `Cadena`, `Datos`, `OtorgadoEn`, `RevocadoEn`, `Consultas` y `UltimaConsulta`. |
| `POST /api/portabilidad/consentimientos` | `cadenaId` y `datos` (lista de códigos autorizables) | El consentimiento registrado con sus datos, fechas y estado. |
| `PUT /api/portabilidad/consentimientos/{id}/revocar` | Sin cuerpo | El consentimiento actualizado con `RevocadoEn` y estado revocado. |
| `GET /api/portabilidad/consentimientos/{id}/consultas` | Sin cuerpo | Lista de consultas con `ConsultadoEn` y `ConsultadoPor`. |

El formulario del POST usa los códigos aceptados por `PortabilidadService`: `PERFIL`,
`RESTRICCIONES`, `HISTORIAL` y `METRICAS`. La pantalla no accede directamente a la base de datos
ni utiliza las rutas administrativas de portabilidad.

## Archivos involucrados

- `movil/GestionGimnasios.Movil/Paginas/Cliente/ConsentimientoPage.cs`: interfaz, estados, acciones
  y llamadas a los cinco endpoints del socio.
- `movil/GestionGimnasios.Movil/Paginas/Comun/MasPage.cs`: acceso para socios desde el menú Más.
- `movil/GestionGimnasios.Movil/Modelos/Modelos.cs`: modelos `CadenaDisponible`, `Consentimiento` y
  `ConsultaPerfil`, alineados con las respuestas del backend.
- `web/GestionGimnasios.Web/Controllers/Api/PortabilidadApi.cs`: definición de las rutas consumidas.
- `web/GestionGimnasios.Web/Services/PortabilidadService.cs`: contratos, validación de datos y reglas
  del consentimiento.

## Prueba manual en Android

1. Iniciar sesión en el emulador con un usuario socio.
2. Abrir **Más → Autorizaciones de perfil**.
3. Seleccionar una cadena, marcar uno o más datos y pulsar **Registrar autorización**.
4. Comprobar que aparece la cadena con estado **Vigente**, fecha y datos autorizados.
5. Pulsar **Ver consultas** para consultar quién accedió al perfil y cuándo. Si no existen accesos,
   se muestra el estado vacío correspondiente.
6. Pulsar **Revocar**, confirmar la acción y comprobar que la autorización queda como **Revocada**.
7. Deslizar para refrescar y verificar que el estado se conserva actualizado.

La compilación Android se realizó correctamente con `dotnet build movil/GestionGimnasios.Movil -f
net10.0-android -v normal -m:1 -nr:false`, con 0 advertencias y 0 errores. El flujo principal fue
comprobado manualmente en el emulador Android.
