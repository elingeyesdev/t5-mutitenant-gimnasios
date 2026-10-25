# Planificación del proyecto · Fase 2: mejoras y entrega

Plan de las iteraciones 1 a 6, tomado del documento de cimientos de la pareja (EDT, PERT, CPM,
casos de uso e historias). La fase 1, la migración del proyecto base al Stack B, quedó cerrada en la
**Iteración 0** con el release `v0.1.0`. Su plan se conserva en
[fase-1-migracion/](fase-1-migracion/README.md) y su resultado en [../migracion.md](../migracion.md).

- Inicio: jueves 15/10/2026 · Último release: miércoles 25/11/2026 · Presentación final: **jueves 26/11/2026**.
- Resultado PERT/CPM, generado: [pert-cpm.md](pert-cpm.md).
- Casos de uso y reglas: [../diseno/casos-de-uso.md](../diseno/casos-de-uso.md).
- Historias de usuario: [../diseno/historias-de-usuario.md](../diseno/historias-de-usuario.md).

## 1. Alcance

| RF | Mejora | Historia | Caso de uso |
|---|---|---|---|
| RF-01 | Análisis de técnica a partir de un video, con revisión y valoración final del entrenador | HU-46 | CU-35 |
| RF-02 | Portabilidad del perfil deportivo con consentimiento explícito del socio | HU-47 | CU-36 |
| RF-03 | Recompensas mediante vales, cupones y reglas verificables de canje | HU-48 | CU-37 |
| RF-04 | Alertas tempranas de inactividad o abandono para el seguimiento del personal | HU-49 | CU-38 |
| RF-05 | Pago y renovación con QR, con verificación del estado de la operación | HU-50 | CU-39 |
| RF-06 | Simulaciones visuales de ejercicios e integración con el plan nutricional | HU-51 | CU-40 |
| — | Revisión del diseño visual pedida por el docente: interfaz oscura y coherente | HU-52 | CU-01 a CU-34 |

Además entran las pruebas integrales, la seguridad de acceso por cadena, la documentación y la
presentación final.

**Integraciones sujetas a aprobación.** El análisis de video, las simulaciones 3D, el cobro QR real
y los servicios de notificación usan tecnologías que no están en la guía del curso. Sin aprobación o
sin credenciales externas, la iteración entrega un **flujo demostrable con evidencias**, sin afirmar
un procesamiento real que no existe.

## 2. EDT

| EDT | Entregable | Actividades | Responsable | Historia |
|---|---|---|---|---|
| 1.0 | Plan de alcance y criterios de aceptación | A | Pareja | HU-46 a HU-52 |
| 2.0 | Diseño visual web y móvil | B, C, D | Jean (B, D) · Melanie (C) | HU-52 |
| 3.0 | Perfil deportivo portable (RF-02) | E, F | Jean (E) · Melanie (F) | HU-47 |
| 4.0 | Recompensas y cupones (RF-03) | G, H | Jean (G) · Melanie (H) | HU-48 |
| 5.0 | Alertas de abandono (RF-04) | I, J | Jean | HU-49 |
| 6.0 | Pago y renovación QR (RF-05) | K, L | Jean (K) · Melanie (L) | HU-50 |
| 7.0 | Revisión de técnica asistida (RF-01) | M, N, O | Jean (M, N) · Melanie (O) | HU-46 |
| 8.0 | Simulaciones y nutrición (RF-06) | P, Q, R | Melanie (P, Q) · Jean (R) | HU-51 |
| 9.0 | Pruebas, documentación y release | S, T | Pareja | HU-01 a HU-52 |

**Definición de «Hecho» de una actividad:**
- se cumplen sus criterios de aceptación;
- el PR hacia `develop` está aprobado por la compañera, con el CI en verde;
- el issue se cerró con `Closes #N`;
- las pruebas de regresión de HU-01 a HU-45 siguen pasando.

## 3. Cómo se estimó

- **PERT de tres puntos** por actividad: TE = (O + 4M + P) ÷ 6 y σ = (P − O) ÷ 6, en días de
  trabajo. Ninguna actividad supera 3 días esperados, para poder seguirlas semana a semana.
- **Precedencias:**
  - técnicas: la app consume una API ya validada;
  - por recurso: las tareas de Jean van en la secuencia B-D-E-G-I-J-K-M-N-R y las de Melanie en
    C-F-H-L-O-P-Q;
  - A, S y T se hacen en pareja.
- **CPM:** duración de **37 días**, con ruta crítica **A-B-D-E-G-I-J-K-M-N-O-P-Q-S-T**. Las seis
  iteraciones suman 42 días, así que quedan **5 días de reserva** para correcciones, aprobaciones
  externas o ajustes. No es tiempo libre asegurado.
- Los datos están en [actividades.csv](actividades.csv). Para regenerar el resultado:
  ```
  cd docs/planificacion
  dotnet run PertCpm.cs
  ```

## 4. Iteraciones

Semana de **jueves a miércoles**:
- **Jueves:** revisión con el docente y planning: se mueven a *Ready* las actividades de la semana.
- **Viernes a martes:** desarrollo, pruebas y PR con revisión cruzada.
- **Miércoles:** estabilización, release (`develop` → `main`, tag `v0.N.0`, APK) y retrospectiva.

| Iteración | Inicio | Release | Objetivo | Actividades | Versión |
|---|---|---|---|---|---|
| 0 · Migración | 08/10 | 12/10 | Migración al Stack B (cerrada) | HU-01 a HU-45 | v0.1.0 |
| 1 | 15/10 | 21/10 | Criterios, arquitectura visual y prototipo oscuro | A, B, C, D | v0.2.0 |
| 2 | 22/10 | 28/10 | Cierre visual, RF-02 y RF-03; inicio de alertas | E, F, G, H, I | v0.3.0 |
| 3 | 29/10 | 04/11 | RF-04 alertas, RF-05 pagos QR; inicio de RF-01 | J, K, L, M | v0.4.0 |
| 4 | 05/11 | 11/11 | RF-01 validación humana y app; inicio de RF-06 | N, O, R | v0.5.0 |
| 5 | 12/11 | 18/11 | RF-06 simulaciones y nutrición; pruebas integrales | P, Q, S | v0.6.0 |
| 6 | 19/11 | 25/11 | Correcciones finales, manual, release y margen técnico | T | v1.0.0 |

**Seguimiento: plan frente a ejecución.** La revisión de la Iteración 1 pasó al **lunes 26/10** y la
de la Iteración 2 al **jueves 29/10**; los milestones de GitHub tienen esas fechas. Las actividades
A a D, planificadas del 15 al 20/10, se ejecutan del 25 al 26/10. El atraso se absorbe con la reserva
de 5 días, y cualquier desvío mayor se revisa con el docente con un ajuste explícito de alcance.

## 5. GitHub

| Concepto del plan | En GitHub |
|---|---|
| Iteración | **Milestone**: *Iteración 0 · Migración* (cerrado) e *Iteración 1* a *6* |
| Actividad A–T de la EDT | **Issue** #50 a #69, con título `[EDT] Nombre`; en el cuerpo, la historia, los criterios, el PERT, las predecesoras y las fechas del CPM |
| Historias HU-01 a HU-45 (migración) | Issues cerrados en la Iteración 0 |
| Producto | **Etiquetas** `Web` y `Movil` (más `bug` para errores) |
| Responsable | **Assignee**: AchoMendez (web) y meldiazo (móvil) |
| Tablero | **Project #12**: Backlog → Ready (iteración) → In progress → In review (PR) → Done |

**Flujo de cada actividad:**
1. issue en *Ready*;
2. rama `feature/<issue>-<descripcion>` desde `develop`;
3. commits `feat(web): …` o `feat(movil): …`;
4. PR hacia `develop` con `Closes #<issue>`;
5. revisión y aprobación de la compañera, con el CI en verde;
6. *Squash and merge*.

**Definición de «Listo»**, antes de programar:
- caso de uso validado;
- criterios de aceptación escritos;
- diseño de la pantalla cuando corresponda;
- estimación PERT;
- issue asignado en el Project.

## 6. Decisiones por validar con el docente

1. Aprobación del diseño visual oscuro y de la plantilla de administración definitiva.
2. Tecnología admisible para procesar videos y forma de revisión del entrenador (RF-01).
3. Alcance del consentimiento y datos que se pueden compartir del perfil portable (RF-02).
4. Reglas concretas de acumulación y canje de recompensas (RF-03).
5. Umbrales de inactividad y responsables del seguimiento (RF-04).
6. Proveedor o forma de demostración del pago QR y su verificación (RF-05).
7. Tecnología y profundidad de las simulaciones 3D (RF-06).
8. Prioridad explícita de las seis mejoras, si una dependencia o aprobación externa amenaza el plazo.
