# Captura y seguimiento de videos de técnica

## Identificación

- **Issue:** #64.
- **Requisito funcional:** RF-01 · captura y seguimiento del resultado en la aplicación móvil.
- **Historia de usuario:** HU-46.
- **Caso de uso:** CU-35 · revisión de la técnica por video.
- **Alcance:** envío desde la app MAUI y devolución del entrenador desde el panel web.

## Objetivo y navegación

La funcionalidad permite que un socio envíe un video de un ejercicio a su entrenador, consulte el
estado del envío y reciba la valoración y la observación final cuando el entrenador termine la
revisión.

El acceso para socios está disponible en **Más → Revisión de técnica**. La opción se agrega en
`movil/GestionGimnasios.Movil/Paginas/Comun/MasPage.cs` y abre
`movil/GestionGimnasios.Movil/Paginas/Cliente/VideosTecnicaPage.cs`.

## Pantalla móvil

`VideosTecnicaPage` consulta los ejercicios disponibles y el historial personal. La pantalla incluye:

- selector de ejercicio;
- comentario opcional de hasta 500 caracteres;
- selección de un video desde el almacenamiento de Android;
- grabación directa con cámara y micrófono;
- envío del video;
- historial con estado, fecha, comentario, valoración y observación final.

Mientras se envía el archivo, el botón queda deshabilitado para evitar solicitudes repetidas. La
pantalla también muestra indicador de carga, estados vacíos y permite actualizar el historial con el
refresco manual.

Los formatos aceptados por la app y el backend son MP4, MOV y WEBM, con un máximo de 50 MB. El
backend comprueba además la firma del archivo; para MP4 exige la caja `ftyp`, por lo que no basta con
cambiar la extensión de un archivo que no sea video.

Para grabar, la app solicita permisos de cámara y micrófono. Si el usuario cancela el selector, no
otorga permisos o se produce un error, se muestra el mensaje correspondiente sin enviar el video.

## Integración con la API

La app utiliza `movil/GestionGimnasios.Movil/Servicios/Api.cs`, que agrega el token JWT de la sesión
a las solicitudes.

| Método y ruta | Contrato utilizado |
|---|---|
| `GET /api/ejercicios` | Devuelve una lista de `EjercicioDto`; la app utiliza principalmente `Id` y `Nombre` para el selector. |
| `GET /api/videos-tecnica/mios` | Devuelve `VideoSocioDto` con `Id`, `Ejercicio`, `Comentario`, `Entrenador`, `EnviadoEn`, `Estado`, `Valoracion`, `Observacion` y `RevisadoEn`. |
| `POST /api/videos-tecnica` | Recibe `multipart/form-data` con `archivo`, `ejercicioId` y `comentario`; responde con el video registrado para el socio. |

El servicio web exige una asesoría activa con un entrenador, un ejercicio activo y menos de tres
videos pendientes. Si no existe una asesoría activa, el backend rechaza el envío; la app muestra el
error sin intentar reproducir esa regla localmente.

Los modelos móviles están en
`movil/GestionGimnasios.Movil/Modelos/Modelos.cs`. `VideoTecnica` traduce los estados `PENDIENTE` y
`REVISADO`, y muestra la valoración solo cuando el video ya fue revisado.

## Revisión desde el panel web

El entrenador trabaja desde **Gestión → Videos de técnica**. Los archivos principales son:

- `web/GestionGimnasios.Web/Controllers/VideosTecnicaController.cs`: lista, detalle, archivo y
  envío de la devolución desde el panel.
- `web/GestionGimnasios.Web/Views/VideosTecnica/Index.cshtml`: lista filtrable por videos pendientes
  o revisados.
- `web/GestionGimnasios.Web/Views/VideosTecnica/Detalle.cshtml`: reproducción del video, análisis
  preliminar y formulario de devolución.
- `web/GestionGimnasios.Web/Services/VideosTecnicaService.cs`: validaciones, permisos de acceso,
  registro de la revisión y notificaciones.

Las rutas del panel son MVC, no endpoints móviles: `VideosTecnica/Index`,
`VideosTecnica/Detalle/{id}`, `VideosTecnica/Archivo/{id}` y el POST de `VideosTecnica/Revisar/{id}`.
Solo el entrenador asignado puede consultar los videos mientras la asesoría permanezca activa.

## Análisis preliminar y privacidad

`web/GestionGimnasios.Web/Services/AnalisisVideo.cs` lee únicamente metadatos del archivo: duración
y resolución. Considera como referencia una duración de 3 a 180 segundos y un lado mínimo de 360
píxeles. Si los datos no cumplen esas referencias, genera observaciones para el entrenador.

Este análisis no detecta posturas, movimientos ni técnica corporal. El entrenador debe confirmarlo
o corregirlo, elegir una valoración (`ADECUADA`, `MEJORABLE` o `INCORRECTA`) y
escribir una observación de hasta 1000 caracteres. El socio recibe únicamente la valoración y la
observación final; el análisis preliminar y sus correcciones permanecen en la vista del entrenador.

## Prueba E2E comprobada el 16/11/2026

1. Se compiló la app MAUI para Android ARM64 y se generó una APK funcional sin Fast Deployment.
2. Carlos López inició sesión desde el emulador `GestionGimnasios_API33`, conectado al backend
   local mediante `http://10.0.2.2:5253`.
3. Se abrió **Más → Revisión de técnica** y se verificaron los campos, botones e historial.
4. Se seleccionó un MP4 artificial de 5 segundos, H.264 y 320×240, generado localmente con FFmpeg.
5. Se eligió **Sentadilla Libre** y se agregó un comentario.
6. El primer envío fue rechazado correctamente porque Carlos no tenía una asesoría activa.
7. Carlos solicitó asesoría a Carmen Cabrera y Carmen la aceptó desde su panel móvil.
8. Carlos envió el video y este apareció como pendiente en su historial.
9. Carmen recibió la notificación, abrió el video y lo reprodujo desde el panel web.
10. Carmen visualizó el análisis preliminar, que informó observaciones por la resolución baja.
11. Carmen confirmó el análisis, eligió **Puede mejorar** y registró una devolución de prueba.
12. Carlos actualizó la pantalla MAUI y visualizó el estado **Revisado**, la valoración y la
    observación final.
13. La pantalla del socio no mostró el análisis preliminar privado.

## Limitaciones verificadas

- No se comprobó satisfactoriamente la grabación directa mediante la cámara del emulador.
- El video utilizado fue artificial; no se validó análisis de técnica corporal real.
- No se ejecutó una batería completa de pruebas negativas ni una prueba en un dispositivo Android
  físico.
- La revisión del entrenador se realiza desde el panel web, conforme al flujo implementado.
- Durante pruebas automatizadas hubo ANR del emulador; la prueba E2E final se completó manualmente.

## Archivos principales involucrados

- `movil/GestionGimnasios.Movil/Paginas/Cliente/VideosTecnicaPage.cs`: formulario móvil, selección o
  captura, validaciones de interfaz, envío e historial.
- `movil/GestionGimnasios.Movil/Paginas/Comun/MasPage.cs`: acceso para socios.
- `movil/GestionGimnasios.Movil/Modelos/Modelos.cs`: contratos y textos de estado del historial.
- `movil/GestionGimnasios.Movil/Servicios/Api.cs`: solicitudes autenticadas y carga multipart.
- `web/GestionGimnasios.Web/Controllers/Api/VideosTecnicaApi.cs`: API de envío e historial del socio.
- `web/GestionGimnasios.Web/Services/VideosTecnicaService.cs`: reglas del flujo y seguridad de acceso.
- `web/GestionGimnasios.Web/Services/AnalisisVideo.cs`: lectura de metadatos y observaciones preliminares.
- `web/GestionGimnasios.Web/Controllers/VideosTecnicaController.cs` y
  `web/GestionGimnasios.Web/Views/VideosTecnica/`: revisión desde el panel web.
- `web/GestionGimnasios.Web/Data/Migraciones/20261108202938_VideosTecnica.cs` y
  `web/GestionGimnasios.Web/Data/Migraciones/20261115214030_AnalisisVideo.cs`: estructura de datos
  utilizada por los videos y sus metadatos.
