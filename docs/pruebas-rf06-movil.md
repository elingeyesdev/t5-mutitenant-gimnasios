# Pruebas RF-06 en móvil

## Identificación

- **Issue:** #66.
- **Requisito:** RF-06.
- **Historia de usuario:** HU-51.
- **Caso de uso:** CU-40.
- **EDT:** 8.2, actividad Q del PERT.
- **Rama:** `feature/66-pruebas-simulacion-movil`.
- **Fecha:** 19/11/2026.

## Ambiente

- macOS Apple Silicon.
- .NET SDK `10.0.303`.
- Aplicación .NET MAUI Android, paquete `bo.edu.univalle.gestiongimnasios`.
- AVD `GestionGimnasios_API33`, dispositivo `emulator-5554`, Android API 33.
- URL configurada por la aplicación: `http://10.0.2.2:5253`.
- Compilación Android ARM64: exitosa, 0 errores y 2 advertencias `NU1900` porque NuGet no pudo
  consultar el índice de vulnerabilidades durante el restore.

El emulador arrancó correctamente y ADB lo reconoció. En la primera comprobación el backend aún no
estaba disponible y la aplicación mostró el error real «No se pudo conectar con el servidor
(http://10.0.2.2:5253)». Después de encenderlo manualmente, el host respondió HTTP 401 sin sesión
y el emulador abrió correctamente la conexión TCP hacia `10.0.2.2:5253`.

## Comportamiento implementado

`SimulacionEjercicioPage` busca primero un GIF local, luego una imagen local, después una imagen
remota y finalmente un video externo cuando el contrato devuelve `TipoSimulacion = VIDEO` y un
`YoutubeVideoId` válido. En ese último caso muestra el botón «Ver video del ejercicio» y abre
`https://www.youtube.com/watch?v={id}` mediante el lanzador de Android.

La información de los nueve ejercicios y sus IDs se contrastó con
`web/GestionGimnasios.Web/Data/Seed/ejercicios.json`. No se agregaron enlaces ni se modificaron
datos.

## Fallbacks externos revisados

| Ejercicio | URL real | Resultado observado | Estado |
|---|---|---|---|
| Cinta / Trotadora | `https://www.youtube.com/watch?v=nUM5Ix8v0W8` | Cargó un video titulado «Cinta de correr Reebok Z…»; la imagen muestra una cinta. | **APROBADO** |
| Curl Araña (Spider Curl) | `https://www.youtube.com/watch?v=UDblkPDopgQ` | Cargó «CURL DE BÍCEPS Y VARIANTES CON MANC…». La persona aparece de pie; no se confirmó un spider curl en banco inclinado. | **FALLIDO** |
| Escaladora (Stairmaster) | `https://www.youtube.com/watch?v=nUM5Ix8v0W8` | Es el mismo video de cinta de correr, no una escaladora. | **FALLIDO** |
| Estiramiento Gato-Camello | `https://www.youtube.com/watch?v=WXGBYjIOcN4` | Cargó «Understanding the Abdominal Wall…», con anatomía abdominal; no muestra gato-camello. | **FALLIDO** |
| Face Pull | `https://www.youtube.com/watch?v=iV4XoV8cFMI` | Cargó «The Face Pull»; se observan cable y anatomía del hombro. | **APROBADO** |
| Hip Thrust en Máquina | `https://www.youtube.com/watch?v=34bf7rl-95o` | Cargó «Cómo Hacer el PESO MUERTO PERFECT…»; muestra peso muerto, no hip thrust en máquina. | **FALLIDO** |
| Remo Ergómetro | `https://www.youtube.com/watch?v=nUM5Ix8v0W8` | Es el mismo video de cinta de correr, no un remo ergómetro. | **FALLIDO** |
| Remo en Punta (T-Bar) | `https://www.youtube.com/watch?v=2J61UenaFRw` | Cargó «Así debes hacer el remo con barra». Es un remo genérico; no se confirmó la variante T-Bar. | **FALLIDO** |
| Woodchoppers en Polea | `https://www.youtube.com/watch?v=u0-X63Fq7LU` | Cargó «OBLIQUE CRUNCH (STRAPS)»; no muestra woodchoppers en polea. | **FALLIDO** |

En esta verificación se comprobó la apertura externa de los nueve IDs únicos usados por el catálogo
(el ID `nUM5Ix8v0W8` se reutiliza en Cinta, Escaladora y Remo Ergómetro). El botón dentro de MAUI
no pudo observarse porque la API no respondió; el comportamiento de apertura está definido en
`SimulacionEjercicioPage.cs`.

## Casos RF-06

| Caso | Resultado |
|---|---|
| Abrir un GIF local y comprobar su reproducción | **APROBADO previamente** en las pruebas documentadas del #65; no se repitió porque la API estaba caída. |
| Abrir Plancha Abdominal o Lateral como imagen estática | **APROBADO**; Plancha Abdominal abrió con imagen local. Evidencia: [simulación y nutrición](evidencias/rf06/simulacion-plancha-y-nutricion.png). |
| Ver «Ejercicios de mi rutina» con prescripciones originales | **APROBADO**; se conservaron día, series, repeticiones y descanso. Evidencia: [rutina y catálogo](evidencias/rf06/rutina-y-catalogo.png). |
| Mantener el catálogo general | **APROBADO**; evidencia: [rutina y catálogo](evidencias/rf06/rutina-y-catalogo.png). |
| Búsqueda dinámica del catálogo | **APROBADO después de la corrección**; `jumping`, `plancha` y una consulta sin resultados respondieron correctamente sin nuevo ANR. Evidencias: [jumping](evidencias/rf06/busqueda-jumping.png), [plancha](evidencias/rf06/busqueda-plancha.png) y [sin resultados](evidencias/rf06/busqueda-sin-resultados.png). |
| Escritura, borrado y reemplazo rápido | **APROBADO en estabilidad**; no apareció ANR, aunque la entrada rápida por ADB dejó una consulta mezclada y no se usa para validar el texto final. Evidencia: [búsqueda rápida](evidencias/rf06/busqueda-rapida.png). |
| Mostrar pautas nutricionales reales del plan | **APROBADO**; las pautas del plan se mantuvieron visibles. Evidencia: [simulación y nutrición](evidencias/rf06/simulacion-plancha-y-nutricion.png). |
| Socio sin plan y objetivos no especificados | **NO EJECUTADO**; no había datos adecuados disponibles y la API no respondió. |
| Inicio de aplicación y conexión al backend | **APROBADO** después de iniciar el backend; la APK abrió y conservó la sesión de Carlos. |

## Evidencias incorporadas

Las siguientes capturas reales del emulador se copiaron a
[docs/evidencias/rf06/](evidencias/rf06/) para que puedan visualizarse desde GitHub:

- [Rutina asignada y catálogo](evidencias/rf06/rutina-y-catalogo.png).
- [Búsqueda `jumping`](evidencias/rf06/busqueda-jumping.png).
- [Búsqueda `plancha`](evidencias/rf06/busqueda-plancha.png).
- [Consulta sin resultados](evidencias/rf06/busqueda-sin-resultados.png).
- [Escritura rápida sin ANR](evidencias/rf06/busqueda-rapida.png).
- [Simulación estática y pautas nutricionales](evidencias/rf06/simulacion-plancha-y-nutricion.png).
- [Fallback de Cinta / Trotadora](evidencias/rf06/fallback-cinta.png), resultado aprobado.
- [Fallback de Face Pull](evidencias/rf06/fallback-face-pull.png), resultado aprobado.

Los siete fallos de YouTube permanecen documentados en la tabla de fallbacks y no se consideran
aprobados. Las capturas temporales de esos fallos no se copiaron al repositorio porque no aportan
una demostración válida del ejercicio.

## Limitaciones y pendientes

- Confirmar dentro de la aplicación la visualización del botón de video para cada ejercicio.
- Los siete enlaces incorrectos de YouTube permanecen como FALLIDOS; su reemplazo queda pendiente.
- Verificar nuevamente el caso sin plan y objetivos no especificados con datos reales adecuados.
- Las demostraciones externas dependen de YouTube y de la conectividad del emulador; no son recursos
  locales ni controlados por la aplicación.
- La aprobación docente permanece pendiente.
- La validación profesional de las demostraciones permanece pendiente.

## Diagnóstico del ANR

### Causa y evidencia

El ANR original del 19/11/2026 fue un timeout de `KeyEvent` de 5002 ms mientras se usaba el
buscador. El segundo ANR fue un timeout de `FocusEvent` de 5011 ms durante la navegación hacia
«Entrenar», con 686 cuadros omitidos y un `Davey! duration=11825ms`. No se observaron excepciones
C#, `FATAL EXCEPTION` ni `SIGABRT` en esos eventos.

La revisión confirmó un factor de rendimiento concreto en la implementación anterior:
`TextChanged` llamaba sincrónicamente a `FiltrarEjercicios`, que ejecutaba
`BindableLayout.SetItemsSource` para reconstruir aproximadamente 85 tarjetas dentro de un
`ScrollView` no virtualizado. `TextoEjercicio.Ordenar` solamente normaliza, filtra y ordena; no hace
llamadas de red ni acceso a disco.

La reconstrucción completa en cada tecla queda confirmada como una fuente de trabajo excesivo del
hilo de interfaz. La traza nativa y la presión de memoria del emulador no permiten afirmar que sea
la única causa del ANR original, pero sí explican técnicamente el bloqueo y la caída de cuadros.

### Corrección aplicada

En `MiRutinaPage.xaml` se reemplazó el `ScrollView` con listas dinámicas por un único
`CollectionView`. Sus secciones se conservaron mediante `Header` y `Footer`:

- `Header`: rutina asignada, botones de entrenamiento, ejercicios de la rutina y buscador.
- Elementos virtualizados: catálogo de todos los ejercicios.
- `Footer`: «Últimos entrenamientos» e historial.

En `MiRutinaPage.xaml.cs` se aplicó un debounce de 180 ms con cancelación de filtros anteriores. El
`ItemsSource` del catálogo se asigna una sola vez a una colección observable estable; cada consulta
publica un único reinicio de esa colección y restaura el foco del `SearchBar`. No se hacen llamadas
a la API por tecla. `TextoEjercicio.cs` y sus reglas de normalización, acentos, coincidencias
iniciales y orden alfabético no fueron modificados.

### Verificación Android

La primera APK posterior al cambio de layout no era válida para Debug autocontenido: logcat mostró
`No assemblies found ... .__override__` y bibliotecas `lib/arm64-v8a` comprimidas. Se descartó esa
instalación como evidencia funcional. La APK final se compiló con ARM64, sin Fast Deployment,
`EmbedAssembliesIntoApk=true` y bibliotecas `.so` almacenadas sin compresión. La instalación con
`adb install -r` terminó en `Success` y conservó la sesión de Carlos.

Resultado de la compilación final: **EXITOSA**, 0 errores y 2 advertencias `NU1900` por falta de
acceso al índice de vulnerabilidades de NuGet. El paquete final fue:
`movil/GestionGimnasios.Movil/bin/Debug/net10.0-android/bo.edu.univalle.gestiongimnasios-Signed.apk`.

### Casos ejecutados después de la corrección

| Caso | Resultado | Evidencia |
|---|---|---|
| Abrir Entrenar con Carlos y cargar rutina/catalogo | **APROBADO** | [rutina y catálogo](evidencias/rf06/rutina-y-catalogo.png) |
| Conservar prescripción de Sentadilla Libre | **APROBADO**: lunes, 3 series × 10, descanso 60 s | [rutina y catálogo](evidencias/rf06/rutina-y-catalogo.png) |
| Escribir `jumping` con teclado y mantener foco | **APROBADO**: texto completo y resultado Jumping Jacks | [búsqueda jumping](evidencias/rf06/busqueda-jumping.png) |
| Buscar `plancha` | **APROBADO**: Plancha Abdominal y Plancha Lateral | [búsqueda plancha](evidencias/rf06/busqueda-plancha.png) |
| Consulta sin resultados `zzzz` | **APROBADO**: mensaje «No se encontraron ejercicios» | [búsqueda sin resultados](evidencias/rf06/busqueda-sin-resultados.png) |
| Escribir/borrar/reemplazar rápidamente | **APROBADO**: no hubo ANR; el canal ADB dejó una consulta mezclada, por lo que no se usa para validar el texto final | [búsqueda rápida](evidencias/rf06/busqueda-rapida.png) |
| Abrir un ejercicio y regresar | **APROBADO**: abrió Plancha Abdominal y volvió al catálogo | [simulación estática](evidencias/rf06/simulacion-plancha-y-nutricion.png) |
| Imagen estática y pautas nutricionales | **APROBADO** en Plancha Abdominal | [simulación y nutrición](evidencias/rf06/simulacion-plancha-y-nutricion.png) |

En el logcat limpio de la prueba final no aparecieron nuevos `ANR`, `SIGABRT`, `FATAL EXCEPTION` ni
timeouts de entrada. Solo se observó un aviso aislado de 41 cuadros omitidos, sin pérdida de
interactividad ni diálogo de ANR. «Últimos entrenamientos» siguió visible debajo del catálogo,
incluyendo su registro real.

Estado actualizado de la búsqueda dinámica: **APROBADO en la prueba representativa Android**.
La causa exacta del ANR histórico no puede atribuirse únicamente al código porque el emulador
también presentó presión de memoria; sin embargo, el factor de reconstrucción no virtualizada fue
corregido y no se reprodujo el ANR en los casos ejecutados.
