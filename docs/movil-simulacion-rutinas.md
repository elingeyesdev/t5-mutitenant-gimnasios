# RF-06: simulaciones visuales de ejercicios y nutrición en móvil

## Identificación

- **Issue:** #65 · PR #90.
- **Requisito funcional:** RF-06 · simulaciones visuales de ejercicios e integración con el plan nutricional.
- **Historia de usuario:** HU-51.
- **Caso de uso:** CU-40 · consultar simulaciones de ejercicios y nutrición.
- **EDT:** Actividad P · 8.1 · contenido de rutinas, simulación y nutrición.
- **Rama:** `feature/65-movil-simulacion-rutinas`.

## Objetivo y alcance

La aplicación permite que el socio consulte el catálogo de ejercicios, abra el detalle de un
ejercicio y vea una demostración visual junto con las pautas nutricionales de su plan asignado.
También conserva el acceso a la rutina, el entrenamiento libre y el historial de sesiones.

La demostración es visual: puede ser un GIF local, una imagen local o un recurso remoto existente.
No existe análisis de postura, detección biomecánica ni procesamiento real mediante inteligencia
artificial. Las ilustraciones son orientativas y su uso como material de instrucción requiere la
aprobación docente y una revisión profesional.

Los criterios de aceptación se interpretan así:

1. La simulación se muestra en el dispositivo Android aprobado cuando el ejercicio tiene un recurso
   disponible.
2. Las pautas nutricionales pertenecen al plan escrito y asignado por un profesional; la aplicación
   no genera recomendaciones clínicas automáticas.

## Flujo de uso del socio

### Consultar ejercicios

1. Abrir la pestaña **Entrenar**.
2. Ver la rutina asignada, si existe.
3. Consultar **Ejercicios**, que carga todos los ejercicios activos del catálogo mediante
   `GET /api/ejercicios`, no únicamente los incluidos en la rutina.
4. Buscar por nombre. La búsqueda ignora mayúsculas y acentos, y ordena primero las coincidencias
   iniciales, después las coincidencias internas y finalmente por nombre.
5. Pulsar **Ver ejercicio**.
6. Consultar la demostración disponible y las pautas nutricionales.

La pantalla de detalle es `SimulacionEjercicioPage`. Incluye la información del ejercicio, la
demostración, el plan nutricional y un `ScrollView` para acceder a todo el contenido.

### Entrenamiento libre

1. Abrir **Entrenar**.
2. Pulsar **Entrenamiento libre**.
3. Pulsar **Agregar ejercicio**.
4. Buscar en `SelectorEjercicioPage`.
5. Seleccionar un ejercicio.
6. Continuar con el registro habitual de series, duración, peso, distancia y notas.

La sesión evita agregar dos veces el mismo ejercicio mediante su identificador. Esta mejora del
selector no cambia el contrato del registro de entrenamientos.

### Revisión de técnica

1. Ir a **Más → Revisión de técnica**.
2. Pulsar **Seleccionar ejercicio**.
3. Buscar y seleccionar un ejercicio en `SelectorEjercicioPage`.
4. Continuar con el flujo existente de selección, grabación o envío del video.

El selector buscable mejora la selección del ejercicio, pero no equivale a haber validado la
grabación y el envío en un dispositivo Android físico. Ese flujo pertenece al issue #64.

## Integración con la API y nutrición

La aplicación utiliza `movil/GestionGimnasios.Movil/Servicios/Api.cs`, que agrega la autenticación
de la sesión a las solicitudes.

| Método y ruta | Uso real |
|---|---|
| `GET /api/ejercicios` | Catálogo de ejercicios activos para Entrenar, entrenamiento libre y revisión de técnica. |
| `GET /api/rutinas/mias` | Rutinas del socio; se muestra la rutina activa o la primera disponible. |
| `GET /api/simulaciones/ejercicios/{id}` | Detalle del ejercicio, tipo de demostración, datos remotos disponibles y pautas del plan vigente. |
| `GET /api/entrenamiento/sesiones?limite=5` | Historial breve mostrado en Entrenar. |
| `POST /api/entrenamiento/sesiones` | Registro habitual de una sesión terminada. |
| `GET /api/videos-tecnica/mios` | Historial usado por Revisión de técnica; el envío existente utiliza `POST /api/videos-tecnica`. |

El endpoint de simulación devuelve `SimulacionDto` con el ejercicio, grupo muscular, equipo,
dificultad, `TipoSimulacion`, `YoutubeVideoId`, `ImagenUrl`, `Nutricion` y `AvisoNutricion`.

`SimulacionesService` obtiene el plan nutricional vigente del socio a partir de sus asesorías
activas y copia únicamente los datos escritos por el profesional. En la pantalla se muestran:

- objetivos diarios y macronutrientes, o **Objetivos nutricionales no especificados** cuando no hay
  valores de macros;
- pauta **Antes de entrenar**;
- pauta **Después de entrenar**;
- **Hidratación**;
- profesional responsable y fecha de actualización.

Si el socio no tiene plan o el plan no contiene datos utilizables, aparece el aviso del backend:
**Tu plan todavía no tiene pautas nutricionales. Las define tu nutricionista o tu entrenador.**

El plan web también puede contener un menú (`PlanComidas`), pero el contrato de `SimulacionDto` no
lo envía y `SimulacionEjercicioPage` no lo presenta en esta implementación. No se documenta como
una función disponible en esta pantalla.

## Recursos visuales y correspondencias

Los recursos provienen de Flow Exercise Dataset. El aviso completo de procedencia, IDs, archivos,
licencia declarada y limitaciones está en:

`movil/GestionGimnasios.Movil/Resources/Raw/FlowExerciseDatasetNotice.txt`

La licencia declarada para los cuadros SVG es CC0-1.0, en la medida de los derechos que la fuente
puede dedicar. Se copiaron únicamente recursos convertidos para la aplicación; no se incorporó el
repositorio externo completo. La licencia declarada no garantiza derechos de terceros ni una
validación profesional de la técnica.

Los GIFs locales tienen estas características:

- resolución `320×320`;
- fondo oscuro `#151D26`;
- 350 ms por cuadro;
- bucle infinito;
- secuencia habitual `0,1,2,1`;
- excepciones documentadas `0,1,0,1` cuando el tercer cuadro era defectuoso o incoherente.

Plancha Abdominal y Plancha Lateral se presentan como imágenes estáticas de 320×320. Para Plancha
Lateral se conserva solamente la primera pose de Flow `1775`, que muestra la plancha lateral estándar;
las poses posteriores representan elevación de la pierna.

La relación se hace mediante el nombre normalizado del ejercicio en
`SimulacionEjercicioPage.cs`, no mediante el ordinal del seeder. La prioridad real de presentación es:

1. GIF local compatible.
2. Imagen local compatible.
3. Imagen remota válida desde `ImagenUrl`.
4. Video externo de YouTube, únicamente mediante el botón y la acción del usuario.
5. Mensaje **Demostración visual pendiente de incorporar**.

No se afirma que los enlaces de YouTube sean correspondencias correctas para todos los ejercicios
que todavía no tienen recurso local.

### Cobertura auditada

La auditoría actual del seeder y del código confirma:

- 85 ejercicios activos.
- 73 archivos GIF.
- 2 imágenes PNG usadas como demostración.
- 74 ejercicios con GIF asociado.
- 2 ejercicios con imagen estática asociada.
- 1 recurso reutilizado: `simulacion_bicicleta_estatica.gif` para Bicicleta Estática y Bicicleta de Spinning.
- 76 ejercicios con recurso local en total.
- 9 ejercicios pendientes: Cinta / Trotadora, Curl Araña (Spider Curl), Escaladora (Stairmaster),
  Estiramiento Gato-Camello, Face Pull, Hip Thrust en Máquina, Remo Ergómetro, Remo en Punta
  (T-Bar) y Woodchoppers en Polea.

No se encontraron recursos huérfanos, referencias rotas ni nombres base duplicados entre extensiones.

Hip Thrust en Máquina se mantuvo pendiente. Flow `0756` (`smith hip raise`) muestra una variante en
jaula Smith con banco y barra sobre la pelvis; es cercana, pero no equivale de forma exacta a una
máquina específica de Hip Thrust.

## Mejoras de usabilidad incluidas en el PR #90

- Catálogo completo de ejercicios activos en Entrenar, independiente de la rutina asignada.
- Búsqueda dinámica en el catálogo.
- Orden por relevancia: inicio de nombre, coincidencia interna y orden alfabético.
- Normalización de mayúsculas y acentos.
- Actualización del resultado al escribir, conservando el control de búsqueda.
- `ScrollView` para acceder a la rutina, catálogo, historial y pautas sin cortar la pantalla.
- Selector buscable para **Entrenamiento libre → Agregar ejercicio**.
- Selector buscable para **Más → Revisión de técnica**.
- Prevención de ejercicios duplicados en una sesión de entrenamiento libre.
- Conservación de rutina asignada, entrenamiento libre e historial.

## Archivos principales

- `movil/GestionGimnasios.Movil/Paginas/Cliente/SimulacionEjercicioPage.cs`: consulta el detalle,
  selecciona el recurso visual y muestra las pautas nutricionales.
- `movil/GestionGimnasios.Movil/Paginas/Cliente/MiRutinaPage.xaml`: rutina, catálogo, buscador,
  historial y botones de entrenamiento.
- `movil/GestionGimnasios.Movil/Paginas/Cliente/MiRutinaPage.xaml.cs`: carga de rutina, catálogo y
  sesiones; búsqueda y navegación al detalle.
- `movil/GestionGimnasios.Movil/Paginas/Cliente/EntrenarPage.cs`: entrenamiento asignado o libre,
  selector y prevención de duplicados.
- `movil/GestionGimnasios.Movil/Paginas/Cliente/SelectorEjercicioPage.cs`: selector reutilizable con
  búsqueda y `CollectionView`.
- `movil/GestionGimnasios.Movil/Paginas/Cliente/VideosTecnicaPage.cs`: uso del selector dentro de
  Revisión de técnica y flujo existente de videos.
- `movil/GestionGimnasios.Movil/Modelos/TextoEjercicio.cs`: normalización y orden de resultados.
- `movil/GestionGimnasios.Movil/Modelos/Modelos.cs`: modelos de ejercicio, rutina, simulación, plan y
  sesiones.
- `movil/GestionGimnasios.Movil/Resources/Images/`: GIFs e imágenes estáticas locales.
- `web/GestionGimnasios.Web/Controllers/Api/SimulacionesApi.cs`: endpoint consumido por la app.
- `web/GestionGimnasios.Web/Services/SimulacionesService.cs`: selección del plan y contrato de
  simulación; no genera recomendaciones clínicas.

## Verificación reproducible

### Comprobaciones realizadas

- Compilación Android ARM64 sin Fast Deployment con `EmbedAssembliesIntoApk=true`.
- Instalación de la APK en `GestionGimnasios_API33` conservando los datos y la sesión.
- Inicio de la app y navegación desde Entrenar al catálogo y al detalle.
- Reproducción automática y cambio de cuadros comprobado en animaciones GIF, incluida la verificación
  previa de Sentadilla Libre y Jumping Jacks.
- Visualización de las imágenes estáticas de Plancha Abdominal y Plancha Lateral.
- Visualización de pautas nutricionales junto con las demostraciones.
- Búsqueda dinámica del catálogo y apertura de ejercicios.
- Revisión del selector compartido para entrenamiento libre y Revisión de técnica.
- `git diff --check` correcto.

La comprobación de selectores, foco y desplazamiento se complementa con la inspección del código de
`MiRutinaPage`, `SelectorEjercicioPage` y `VideosTecnicaPage`; no se presenta como una batería
automatizada ni como aprobación de todos los casos negativos.

### Pendientes

- Validación formal del issue #66.
- Grabación y envío de video en un dispositivo Android físico.
- Revisión profesional de la técnica representada por las ilustraciones.
- Aprobación docente para el uso de tecnología y recursos externos.
- Apertura individual en Android de todos los recursos locales que no fueron revisados en pantalla.
- Validación de correspondencia de los enlaces de YouTube que permanecen como fallback.

No existe una carpeta local específica de evidencias Android. `docs/diseno/capturas/` contiene capturas
de la propuesta visual web, no resultados de ejecución. Para el PR se deben adjuntar únicamente
capturas o un video real tomado durante la prueba Android; no se generan imágenes de evidencia dentro
de este documento.

## Propuesta de actualización para el PR #90

```markdown
## Historia
Closes #65 · HU-51 · EDT 8.1

## Qué cambió
- Agregué el catálogo completo de ejercicios en Entrenar con búsqueda normalizada y orden por relevancia.
- Incorporé la navegación al detalle de ejercicio con GIFs e imágenes locales de Flow, manteniendo los fallbacks existentes.
- Integré la visualización de las pautas nutricionales del plan asignado, sin generar recomendaciones clínicas automáticas.
- Agregué el selector buscable reutilizable para entrenamiento libre y Revisión de técnica.
- Conservé la rutina asignada, entrenamiento libre, historial y navegación existentes.
- Documenté procedencia, licencias, correspondencias y limitaciones de los recursos visuales.

## Cómo probar
1. Iniciar sesión como socio y abrir Entrenar.
2. Verificar la rutina asignada, el catálogo completo y la búsqueda por nombre o acento.
3. Abrir un ejercicio con "Ver ejercicio" y comprobar la demostración y las pautas nutricionales.
4. Abrir Entrenamiento libre, usar Agregar ejercicio y buscar un ejercicio.
5. Abrir Más → Revisión de técnica y comprobar el selector buscable.
6. Confirmar que no se agregue dos veces el mismo ejercicio en una sesión libre.

## Evidencia
Adjuntar capturas o un video real de la prueba Android realizada en el emulador aprobado.

## Checklist del autor
- [ ] Pruebas automáticas en verde
- [ ] Migraciones de EF Core incluidas (no cambió el modelo en este alcance)
- [x] Documenté pantallas y contratos existentes utilizados
- [x] Sin claves ni cadenas de conexión en el código
```
