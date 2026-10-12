# Planificación del proyecto · Fase 1: migración al Stack B

Flujo del curso: **EDT › PERT › CPM › Planner › GitHub**. Este documento cubre la EDT y las
reglas; el cálculo PERT/CPM está en [pert-cpm.md](pert-cpm.md), generado desde
[actividades.csv](actividades.csv) con `dotnet run PertCpm.cs`.

## 1. Alcance de la fase

Migrar **todo** el proyecto base GymSync (NestJS + React + Expo) a ASP.NET Core + .NET MAUI +
SQL Server, sin agregar funciones, con el diseño multi-tenant descrito en
[../contexto-proyecto.md](../contexto-proyecto.md), y dejarlo funcionando en producción. Al
terminar, se prueba el proyecto migrado completo para verificar que se migró bien (paquete 5.1).

Para tener contra qué comparar, el proyecto base se ejecutó antes tal cual (paquete 2.1,
resultados en [../diagnostico-proyecto-base.md](../diagnostico-proyecto-base.md) §8): lo que
hace hoy es el comportamiento que la versión migrada debe reproducir. Las mejoras RF-01 a RF-06
son la fase 2 (paquete 7 de la EDT).

**Pendientes de aprobación del docente** (funciones del proyecto base que dependen de
tecnologías no nombradas en el PDF; según su respuesta se agregan como paquetes de la EDT o se
reemplazan por la alternativa indicada):

| Función del proyecto base | Motivo |
|---|---|
| Mensajería entre socio y personal (chat) | Tiempo real con Socket.io; equivalente .NET no está en el PDF |
| Mapa y geolocalización de sedes, registro de visitas | Leaflet / ubicación del celular |
| Modo sin conexión (cola offline) | SQLite en el celular |
| Notificaciones push y por correo | Expo Push, Nodemailer |
| Escaneo de QR con la cámara | Librería de cámara; se migra con ingreso manual del código |
| Exportación de reportes a PDF | Librería de PDF; los reportes se muestran en pantalla y se imprimen desde el navegador |

## 2. EDT

Regla del 100 %: incluye todo el trabajo de la fase y nada más. Nombres en sustantivo
(entregables). Cada paquete cabe en una iteración (TE ≤ 5 días, ver validación en pert-cpm.md).

```
1.0  Gestión Gimnasios · Migración al Stack B
├── 1  Gestión
│   ├── 1.1  Plan del proyecto (EDT, PERT, CPM) ........................ B
│   └── 1.2  Repositorio y tablero Planner .............................. C
├── 2  Análisis y diseño
│   ├── 2.1  Diagnóstico del proyecto base (ejecutado tal cual) ......... A
│   ├── 2.2  Requisitos: casos de uso e historias ....................... D
│   ├── 2.3  Modelo de datos multi-tenant ............................... E
│   ├── 2.4  Mockups de la web .......................................... F
│   ├── 2.5  Mockups de la app .......................................... G
│   └── 2.6  Contrato de la API ......................................... H
├── 3  Aplicación web  (web/ · Jean)
│   ├── 3.1  Base del panel, autenticación y roles ...................... W01
│   ├── 3.2  Cadenas y sucursales ....................................... W02
│   ├── 3.3  Usuarios y membresías ...................................... W03
│   ├── 3.4  Personal y asignaciones .................................... W04
│   ├── 3.5  Ejercicios y rutinas ....................................... W05
│   ├── 3.6  Entrenamiento y métricas ................................... W06
│   ├── 3.7  Actividades y horarios ..................................... W07
│   ├── 3.8  Inventario de máquinas ..................................... W08
│   ├── 3.9  Planes de suscripción y pagos .............................. W09
│   ├── 3.10 Reservas ................................................... W10
│   ├── 3.11 Lista de espera ............................................ W11
│   ├── 3.12 Control de acceso y auditoría .............................. W12
│   ├── 3.13 Plan nutricional y citas ................................... W13
│   ├── 3.14 Notificaciones (web y API) ................................. W14
│   ├── 3.15 Panel de resumen ........................................... W15
│   ├── 3.16 Reportes de operación ...................................... W16
│   └── 3.17 Reportes de gestión ........................................ W17
├── 4  App móvil  (movil/ · Melani)
│   ├── 4.1  Base de la app ............................................. M01
│   ├── 4.2  Autenticación y perfil ..................................... M02
│   ├── 4.3  Rutinas y modo entrenamiento ............................... M03
│   ├── 4.4  Historial de entrenamiento ................................. M04
│   ├── 4.5  Seguimiento y objetivos .................................... M05
│   ├── 4.6  Vistas del entrenador ...................................... M06
│   ├── 4.7  Inicio y sedes ............................................. M07
│   ├── 4.8  Reservas y carnet QR ....................................... M08
│   ├── 4.9  Mi plan y plan nutricional ................................. M09
│   ├── 4.10 Vistas del instructor y nutricionista ...................... M10
│   ├── 4.11 Vistas de recepción y gerencia ............................. M11
│   └── 4.12 Notificaciones (app) ....................................... M12
├── 5  Pruebas y documentación
│   ├── 5.1  Verificación de la migración (pruebas integrales) ......... J
│   └── 5.2  Manual de usuario .......................................... K
├── 6  Despliegue
│   ├── 6.1  Infraestructura de producción (hosting, BD, CI/CD) ......... I
│   └── 6.2  Release de la migración .................................... L
└── 7  Mejoras (fase 2, se detallan al terminar la migración)
    ├── 7.1  Revisión de técnica con IA validada por el entrenador (RF-01)
    ├── 7.2  Perfil deportivo portable (RF-02)
    ├── 7.3  Recompensas con vales y cupones (RF-03)
    ├── 7.4  Alertas tempranas de abandono (RF-04)
    ├── 7.5  Pago y renovación con QR (RF-05)
    └── 7.6  Simulaciones 3D y nutrición integrada (RF-06)
```

El paquete 7 se mantiene en la EDT para cumplir la regla del 100 % del proyecto completo, pero
se planifica de forma gradual: se estima y entra al CPM cuando la migración esté estable,
como pidió el docente («antes de aumentar cosas, primero migrarlo y que funcione»).

### Qué entrega cada paquete de producto (definición de «Hecho»)

Cada paquete W y M se entrega completo, con:

- Entidades y **migración de EF Core** (W) o pantallas con su llamada a la API (M).
- Reglas del proyecto base migradas, con **pruebas automatizadas** de esas reglas.
- Filtro de tenant aplicado y probado (W): una cadena no ve datos de otra.
- Datos de prueba (seed) del módulo.
- Pull Request hacia `develop` con «Cómo probar», probado y aprobado por la compañera o el compañero.

## 3. Cómo se estimó

- **Unidad:** días de trabajo del responsable; se trabaja de lunes a domingo.
- **Base de la estimación:** tamaño real del módulo en el proyecto base (líneas de código,
  endpoints y pantallas). Ejemplo: Reservas es el servicio más grande del backend (2077 líneas,
  14 endpoints), por eso tiene la estimación pesimista más alta (P = 8).
- **Dependencias técnicas:** el paquete necesita el resultado de otro (la app no puede consumir
  una API que no existe; una reserva exige un plan de suscripción).
- **Dependencias por recurso** (marcadas con `*`): la misma persona no hace dos paquetes a la
  vez. Sin ellas, el CPM supondría trabajo en paralelo que nadie puede hacer.
- **Orden:** la web construye primero las APIs que la app necesita (usuarios, rutinas,
  entrenamiento) y deja al final lo que es solo del panel (máquinas, resumen, reportes). Este orden
  acorta la duración en unos 6 días respecto de seguir el orden de los módulos del proyecto base.

Las estimaciones O, M y P son de la pareja: si se ajustan, se cambia `actividades.csv` y se
vuelve a ejecutar el cálculo.

## 4. Iteraciones y Planner

- Iteración = 1 semana de lunes a domingo (7 días de trabajo). Revisión con el docente los
  **lunes** y **jueves**.

| Día | Qué pasa |
|---|---|
| Lunes | Revisión con el docente de la versión publicada el domingo · planning game: tareas de Planner a la iteración, issues y milestone, ruta crítica primero |
| Martes y miércoles | Desarrollo en pareja: una rama por historia, TDD, commits pequeños, PR diarios a `develop` con revisión cruzada |
| Jueves | Revisión con el docente del avance en `develop` · corregir lo observado con `fix/*` |
| Viernes a sábado | Desarrollo y estabilización: pruebas, congelar funciones de la iteración |
| Domingo | Release: PR `develop` › `main`, tag `v0.N.0`, despliegue, APK en `movil/releases/`, retrospectiva |

- El release pasa del viernes del PDF al domingo para que el lunes el docente revise una
  versión en producción, que es la regla «sin producción no hay iteración».
- Lo crítico primero: cada lunes se planifican antes las tareas con etiqueta «Ruta crítica».
- Desde la iteración en que termina I (infraestructura), cada domingo hay versión en producción.

**Tablero de Planner** (un plan para la pareja):

| Elemento | Valor |
|---|---|
| Depósitos | Backlog · Iteración · En progreso · En revisión (PR) · Hecho |
| Etiquetas | Fase 1 a Fase 6 · Ruta crítica · Web · Móvil · Bug |
| Nombre de tarea | `[EDT] #issue Título` (ejemplo: `[3.10] #24 Reservas`) |
| Fechas | Inicio y vencimiento de la sección 5 de pert-cpm.md |
| Lista de comprobación | Criterios de aceptación del issue |
| Asignación | Siempre a los dos integrantes |
