# Casos de uso · Gestión Gimnasios

- **CU-01 a CU-34 (fase 1, migración):** describen lo que el proyecto base (GymSync) ya hacía y que
  la versión en Stack B reproduce. Hoy sirven como pruebas de regresión. En el mapa, su columna EDT
  usa la [EDT de la fase 1](../planificacion/fase-1-migracion/README.md).
- **CU-35 a CU-40 (fase 2, mejoras RF-01 a RF-06):** sección 5. Su columna EDT usa la
  [EDT vigente](../planificacion/README.md).

Fuente de cada caso: endpoints del backend NestJS, pantallas del panel React y de la app Expo, y
la ejecución del proyecto base ([../diagnostico-proyecto-base.md](../diagnostico-proyecto-base.md) §8).

## 1. Actores

El proyecto base ordena los roles por **nivel jerárquico**: un usuario solo gestiona a usuarios de
nivel menor y solo dentro de su alcance (cadena o sucursal).

| Actor | Nivel | Alcance | Canal principal |
|---|---|---|---|
| Super administrador | 10 | Todo el sistema | Web |
| Gerente | 5 | Su cadena y todas sus sucursales | Web y app |
| Recepcionista | 4 | Su sucursal | Web y app |
| Entrenador | 3 | Sus alumnos asesorados | App |
| Nutricionista | 3 | Sus pacientes asesorados | App |
| Instructor | 2 | Las clases que dicta en su sucursal | App |
| Coordinador · Personal de limpieza | 2 · 1 | Roles definidos sin funciones propias en el proyecto base | — |
| Cliente (socio) | 1 | Sus propios datos | App |
| Sistema | — | Tareas programadas (salida automática, recordatorios, alertas) | — |

«Personal» = recepcionista, entrenador, nutricionista o instructor.

## 2. Mapa de casos de uso

| CU | Caso de uso | Actores | Canal | EDT |
|---|---|---|---|---|
| CU-01 | Iniciar sesión | Todos | Web y app | 3.1 · 4.1 |
| CU-02 | Registrarse como cliente | Cliente | App y web | 3.1 · 4.2 |
| CU-03 | Recuperar contraseña | Todos | App | 3.1 · 4.2 |
| CU-04 | Gestionar roles, permisos y parámetros | Super administrador | Web | 3.1 |
| CU-05 | Gestionar cadenas y sucursales | Super administrador, Gerente | Web | 3.2 |
| CU-06 | Gestionar usuarios | Super administrador, Gerente, Recepcionista | Web | 3.3 |
| CU-07 | Actualizar mi perfil | Todos | App | 3.3 · 4.2 |
| CU-08 | Asignar horarios al personal | Gerente, Recepcionista | Web | 3.4 |
| CU-09 | Solicitar un asesor | Cliente | App | 3.4 · 4.6 |
| CU-10 | Responder una solicitud de asesoría | Entrenador, Nutricionista | App | 3.4 · 4.6 |
| CU-11 | Gestionar el catálogo de ejercicios | Super administrador | Web | 3.5 |
| CU-12 | Crear y asignar una rutina | Entrenador | App y web | 3.5 · 4.6 |
| CU-13 | Ejecutar una rutina y registrar el entrenamiento | Cliente | App | 3.6 · 4.3 |
| CU-14 | Registrar medidas físicas | Cliente | App | 3.6 · 4.5 |
| CU-15 | Consultar mi progreso | Cliente | App | 3.6 · 4.4 · 4.5 |
| CU-16 | Gestionar actividades y horarios de clases | Gerente, Recepcionista | Web | 3.7 |
| CU-17 | Registrar asistencia a una clase | Instructor | App | 3.7 · 4.10 |
| CU-18 | Gestionar el inventario de máquinas | Gerente, Recepcionista | Web | 3.8 |
| CU-19 | Gestionar planes de suscripción | Gerente | Web | 3.9 |
| CU-20 | Registrar suscripción y pago de un socio | Recepcionista, Gerente | Web | 3.9 |
| CU-21 | Consultar mi plan | Cliente | App | 3.9 · 4.9 |
| CU-22 | Reservar una clase | Cliente, Recepcionista | App y web | 3.10 · 4.8 |
| CU-23 | Cancelar una reserva | Cliente, Personal | App y web | 3.10 · 4.8 |
| CU-24 | Confirmar el ingreso a una clase con QR | Recepcionista, Instructor | Web y app | 3.10 · 4.8 · 4.11 |
| CU-25 | Inscribirse en la lista de espera | Cliente, Recepcionista | App y web | 3.11 · 4.8 |
| CU-26 | Registrar ingreso y salida de la sucursal | Recepcionista, Sistema | Web y app | 3.12 · 4.8 · 4.11 |
| CU-27 | Consultar la auditoría de accesos | Gerente, Recepcionista | Web y app | 3.12 · 4.11 |
| CU-28 | Definir el plan nutricional de un cliente | Entrenador, Nutricionista | App | 3.13 · 4.9 |
| CU-29 | Gestionar citas nutricionales | Nutricionista | App | 3.13 · 4.10 |
| CU-30 | Recibir y configurar notificaciones | Todos, Sistema | App y web | 3.14 · 4.12 |
| CU-31 | Consultar el panel de resumen | Super administrador, Gerente, Recepcionista | Web y app | 3.15 · 4.11 |
| CU-32 | Generar reportes | Super administrador, Gerente | Web | 3.16 · 3.17 |
| CU-33 | Consultar sucursales y sus clases | Cliente | App | 3.2 · 3.7 · 4.7 |
| CU-34 | Consultar reportes del personal | Entrenador, Instructor | App | 3.15 · 4.6 · 4.10 |
| CU-35 | Revisar la técnica mediante video (RF-01) | Cliente, Entrenador | App y web | 7.1 · 7.2 · 7.3 |
| CU-36 | Autorizar la portabilidad del perfil (RF-02) | Cliente, Gerente | App y web | 3.1 · 3.2 |
| CU-37 | Obtener y canjear recompensas (RF-03) | Cliente, Recepcionista, Gerente | App y web | 4.1 · 4.2 |
| CU-38 | Gestionar alertas de inactividad (RF-04) | Entrenador, Gerente, Sistema | Web | 5.1 · 5.2 |
| CU-39 | Renovar la membresía con QR (RF-05) | Cliente, Recepcionista | App y web | 6.1 · 6.2 |
| CU-40 | Consultar simulaciones de ejercicios y nutrición (RF-06) | Cliente, Entrenador, Nutricionista | App y web | 8.1 · 8.2 · 8.3 |

Funciones del proyecto base **pendientes de aprobación del docente** (dependen de tecnologías fuera
del PDF, ver [../planificacion/fase-1-migracion/README.md](../planificacion/fase-1-migracion/README.md) §1): mensajería, mapa de
sedes con geolocalización y registro de visitas, y modo sin conexión. Se agregan como casos de uso
si el docente las aprueba.

## 3. Reglas transversales

Aplican a todos los casos de uso y se prueban en cada paquete.

- **RT-01 Aislamiento por cadena (tenant):** ningún usuario ve ni modifica datos de otra cadena.
  Referencia medida en el proyecto base: el gerente de «Reyes» ve 22 usuarios de su cadena; el
  super administrador ve los 206.
- **RT-02 Alcance por sucursal:** recepcionista e instructor solo operan sobre su sucursal.
- **RT-03 Jerarquía:** nadie crea, edita ni elimina usuarios de nivel igual o superior al propio,
  ni se elimina a sí mismo.
- **RT-04 Autorización en la API:** toda regla de permisos se valida en el servidor, no solo en la
  pantalla. En el proyecto base hay excepciones a corregir al migrar: los endpoints de suscripciones,
  parámetros del sistema y métricas no exigen rol, y la regla «reservar exige suscripción activa»
  solo la valida la app.
- **RT-05 Cuenta desactivada:** un usuario desactivado no puede iniciar sesión.

## 4. Especificación

### CU-01 Iniciar sesión
- **Actor:** todos · **Canal:** web y app
- **Precondición:** el usuario tiene una cuenta activa.
- **Flujo básico:**
  1. Ingresa correo y contraseña.
  2. El sistema valida las credenciales.
  3. El sistema abre la vista que corresponde a su rol: panel web para el personal administrativo;
     en la app, inicio del cliente o panel del entrenador, instructor, nutricionista o gerente.
- **Alternos:**
  - 2a. Credenciales inválidas: muestra «Credenciales inválidas» y no revela cuál dato falló.
  - 2b. Cuenta desactivada: muestra «Cuenta desactivada».
  - 2c. Más de 10 intentos en un minuto desde el mismo origen: el sistema rechaza nuevos intentos
    temporalmente.
- **Postcondición:** sesión abierta con el rol, la cadena y la sucursal del usuario.
- **Origen:** `POST /auth/login`, `GET /auth/me`, `POST /auth/logout`; LoginScreen (web y app).

### CU-02 Registrarse como cliente
- **Actor:** cliente · **Canal:** app y web
- **Precondición:** no tiene cuenta con ese correo.
- **Flujo básico:**
  1. Abre «Regístrate».
  2. Ingresa nombres, apellidos, correo, contraseña y carnet de identidad.
  3. El sistema crea la cuenta con rol Cliente.
- **Alternos:**
  - 3a. Correo ya registrado: «Ya existe un usuario con este email».
  - 3b. Carnet ya registrado: «El Carnet de Identidad (CI) ya está registrado en otra cuenta».
- **Postcondición:** cuenta activa de cliente; puede iniciar sesión.
- **Origen:** `POST /auth/register`; RegisterScreen (web y app).

### CU-03 Recuperar contraseña
- **Actor:** todos · **Canal:** app
- **Precondición:** tiene una cuenta con correo registrado.
- **Flujo básico:**
  1. Abre «Recupérala» e ingresa su correo.
  2. El sistema genera un código de 6 dígitos con vencimiento y lo envía al correo.
  3. Ingresa el código y la nueva contraseña.
  4. El sistema valida el código y actualiza la contraseña.
- **Alternos:**
  - 1a. Más de 5 solicitudes en 15 minutos: se rechazan temporalmente.
  - 4a. Código inválido o vencido: «Código OTP inválido».
- **Postcondición:** la contraseña cambia y el código queda invalidado.
- **Dependencia:** el envío de correo usa Nodemailer en el proyecto base; requiere aprobación del
  docente para su equivalente (pendiente).
- **Origen:** `POST /auth/forgot-password`, `POST /auth/reset-password`; ForgotPassword y ResetPassword (app).

### CU-04 Gestionar roles, permisos y parámetros
- **Actor:** super administrador · **Canal:** web
- **Flujo básico:**
  1. Abre «Roles».
  2. Consulta los roles con su nivel jerárquico y sus permisos.
  3. Crea o edita un rol, le asigna permisos, o asigna/quita un rol a un usuario.
  4. Consulta y edita los parámetros del sistema (clave y valor).
- **Alternos:**
  - 3a. Un rol de sistema no se elimina.
- **Postcondición:** los cambios rigen desde el próximo inicio de sesión de los usuarios afectados.
- **Origen:** `/roles/*`, `/system/settings/*`; RolesView (web).

### CU-05 Gestionar cadenas y sucursales
- **Actores:** super administrador (todas), gerente (las de su cadena) · **Canal:** web
- **Flujo básico:**
  1. Abre «Sucursales».
  2. El super administrador crea una cadena o una sucursal dentro de una cadena.
  3. Edita nombre, descripción, capacidad máxima, estado (activa, abierta) y ubicación (dirección,
     ciudad, coordenadas).
  4. Define el horario de atención por día de la semana.
  5. Consulta la ocupación actual de cada sucursal.
- **Alternos:**
  - 2a. El gerente no puede crear ni eliminar cadenas o sucursales.
  - 3a. El gerente intenta editar una sucursal de otra cadena: «No tienes jurisdicción sobre esta sucursal».
- **Postcondición:** la sucursal queda disponible para personal, clases, máquinas y reservas.
- **Origen:** `/gyms/*`; SucursalesView, SedesView (web).

### CU-06 Gestionar usuarios
- **Actores:** super administrador, gerente, recepcionista · **Canal:** web
- **Flujo básico:**
  1. Abre «Usuarios» y ve la lista paginada de usuarios de su alcance, con filtros.
  2. Crea un usuario: datos personales, rol y sucursal.
  3. Edita sus datos, rol, sucursal o estado (activo/inactivo).
  4. Elimina un usuario.
- **Alternos:**
  - 2a. El gerente debe elegir una sucursal de su cadena: «Como Gerente, debes seleccionar una
    Sucursal válida de tu Marca».
  - 2b. Correo o carnet repetido: se rechaza.
  - 2c. Rol de nivel igual o superior al propio: se rechaza (RT-03).
  - 4a. Eliminar la propia cuenta: «Medida de seguridad: No puedes eliminar tu propia cuenta».
  - 4b. Usuario fuera de su cadena o sucursal: se rechaza.
- **Postcondición:** el usuario queda asociado a la cadena (membresía) y a la sucursal elegidas.
- **Origen:** `/users/*`; UsuariosView (web).

### CU-07 Actualizar mi perfil
- **Actor:** todos · **Canal:** app
- **Flujo básico:**
  1. Abre «Perfil › Mis datos personales».
  2. Edita teléfono, fecha de nacimiento, género, foto, nombre de usuario, altura, nivel de
     experiencia, condiciones médicas y deportes favoritos.
  3. Registra contactos de emergencia.
  4. El cliente completa su perfil de entrenamiento: preferencias, objetivos y restricciones médicas.
- **Postcondición:** los datos quedan disponibles para su entrenador o nutricionista asesor.
- **Origen:** `PATCH /users/me/profile`, `/training/profile`, `/training/restrictions`,
  `/training/emergency-contacts`; MisDatosPersonales, Ajustes, PerfilMenu (app).

### CU-08 Asignar horarios al personal
- **Actores:** gerente, recepcionista · **Canal:** web
- **Flujo básico:**
  1. Elige a un miembro del personal de su alcance.
  2. Asigna su horario semanal (día, hora de inicio y fin).
  3. El personal consulta su horario semanal en la app.
- **Postcondición:** el horario aparece en la agenda del personal.
- **Origen:** `POST/GET /staff/:userId/schedules`, `GET /staff/me/weekly-schedules`.

### CU-09 Solicitar un asesor
- **Actor:** cliente · **Canal:** app
- **Flujo básico:**
  1. Abre el catálogo de personal de su sucursal (entrenadores y nutricionistas).
  2. Elige uno y envía la solicitud.
  3. Consulta el estado de sus solicitudes.
  4. Puede cancelar una asesoría pendiente o activa.
- **Alternos:**
  - 2a. Se elige a sí mismo: «No puedes solicitarte a ti mismo como asesor».
  - 4a. Asesoría ya cerrada: «Solo se pueden cancelar asesorías activas o pendientes».
- **Postcondición:** solicitud en estado pendiente para el asesor elegido.
- **Origen:** `GET /staff/catalog`, `/staff/advisors/*`; StaffCatalogScreen (app).

### CU-10 Responder una solicitud de asesoría
- **Actores:** entrenador, nutricionista · **Canal:** app
- **Flujo básico:**
  1. Ve las solicitudes pendientes.
  2. Acepta o rechaza.
  3. Al aceptar, el cliente pasa a «Mis alumnos» y el asesor puede fijar su meta de sesiones por semana.
- **Alternos:**
  - 2a. No es el asesor destinatario: «Solo el asesor destinatario puede aceptar esta solicitud».
  - 2b. Solicitud ya respondida: «La solicitud ya está en estado …».
- **Postcondición:** relación de asesoría activa o rechazada.
- **Origen:** `/staff/advisors/requests`, `/staff/advisors/:id/accept|reject|target`; MisAlumnos (app).

### CU-11 Gestionar el catálogo de ejercicios
- **Actor:** super administrador · **Canal:** web
- **Flujo básico:**
  1. Abre «Biblioteca de ejercicios».
  2. Crea o edita un ejercicio: nombre, descripción, grupo muscular principal y secundarios,
     categoría, tipo, equipo requerido, dificultad, video de YouTube e imagen.
  3. Elimina un ejercicio.
- **Postcondición:** el ejercicio está disponible para todas las cadenas al armar rutinas.
- **Origen:** `/exercises/*`; ExerciseLibraryScreen (web).

### CU-12 Crear y asignar una rutina
- **Actor:** entrenador · **Canal:** app y web
- **Precondición:** el cliente es su alumno (asesoría activa).
- **Flujo básico:**
  1. Elige a un alumno.
  2. Crea la rutina: nombre, descripción, dificultad y duración en semanas; agrega ejercicios del
     catálogo con orden, día de la semana, series, repeticiones, peso recomendado, descanso y notas.
     Puede guardarla como plantilla.
  3. La asigna al alumno.
  4. Edita o elimina rutinas propias.
- **Alternos:**
  - 2a. Intenta mover la rutina a otra sucursal: «No puede mover la rutina a otra sucursal».
- **Postcondición:** el alumno ve la rutina en «Mi rutina».
- **Origen:** `/routines/*`; AsignarRutinaScreen (app), RutinasView (web).

### CU-13 Ejecutar una rutina y registrar el entrenamiento
- **Actor:** cliente · **Canal:** app
- **Precondición:** tiene una rutina asignada, o entrena libre.
- **Flujo básico:**
  1. Abre «Iniciar entrenamiento» y elige la rutina.
  2. Ve cada ejercicio con su video.
  3. Registra cada serie: repeticiones y peso; el sistema controla el descanso.
  4. Finaliza y ve el resumen de la sesión.
- **Alternos:**
  - 3a. Entrenamiento libre: elige ejercicios sin rutina asignada.
- **Postcondición:** sesión y series guardadas en su historial.
- **Origen:** `/training/sessions*`; WorkoutMode, WorkoutReady, WorkoutActive, WorkoutSummary,
  EjecutarRutina, RegistroEjercicio (app).

### CU-14 Registrar medidas físicas
- **Actor:** cliente · **Canal:** app
- **Flujo básico:**
  1. Abre «Seguimiento».
  2. Registra peso, porcentaje de grasa, masa muscular y circunferencias (cintura, pecho, cadera,
     brazo, muslo y pantorrilla).
  3. El sistema guarda el registro con fecha y muestra su IMC con la categoría (bajo peso, normal,
     sobrepeso, obesidad) usando la altura de su perfil.
- **Postcondición:** nuevo punto en su historial de métricas.
- **Origen:** `POST /users/me/metrics`, `PATCH /users/me/metrics/circumferences`, `/metrics/*`; SeguimientoScreen (app).

### CU-15 Consultar mi progreso
- **Actor:** cliente · **Canal:** app
- **Flujo básico:**
  1. Abre su cuadro de mando o «Mi evolución física».
  2. Ve el historial de entrenamientos, sus récords de fuerza y la evolución de sus medidas.
  3. Revisa y actualiza sus objetivos.
- **Postcondición:** ninguna (consulta).
- **Origen:** `/training/sessions/strength-records`, `/users/me/metrics*`; CuadroDeMandoCliente,
  WorkoutHistory, HistorialRutina, HistorialMetricas, MisObjetivos (app).

### CU-16 Gestionar actividades y horarios de clases
- **Actores:** gerente (su cadena), recepcionista (su sucursal) · **Canal:** web
- **Flujo básico:**
  1. Abre «Actividades».
  2. Crea una actividad (clase o servicio) en una sucursal: nombre, descripción, duración por
     defecto y si es de acceso libre.
  3. Agrega horarios: día, hora de inicio y fin, cupo máximo, si se repite cada semana y un
     instructor de la misma sucursal.
  4. Edita, desactiva o elimina actividades y horarios.
- **Alternos:**
  - 2a. No elige sucursal: «Debes seleccionar una sucursal para el servicio».
  - 2b. Sucursal de otra cadena: «La sucursal seleccionada no pertenece a tu marca».
  - 3a. Instructor inactivo: «El instructor indicado no está activo».
- **Postcondición:** el horario queda disponible para reservas.
- **Origen:** `/activities/*`; ActividadesView (web).

### CU-17 Registrar asistencia a una clase
- **Actor:** instructor · **Canal:** app
- **Flujo básico:**
  1. Ve su agenda de clases del día.
  2. Abre una clase y ve la lista de reservas.
  3. Marca la asistencia de cada participante.
- **Postcondición:** asistencia registrada; cuenta para sus estadísticas y los reportes.
- **Origen:** `GET /staff/agenda/classes`, `/activities/schedules/:id/attendance`,
  `GET /staff/me/stats/attendance`; InstructorDashboard, ClaseDetalle (app).

### CU-18 Gestionar el inventario de máquinas
- **Actores:** gerente, recepcionista · **Canal:** web
- **Flujo básico:**
  1. Abre «Máquinas» de una sucursal.
  2. Registra una máquina: nombre, categoría (cardio, tren superior, tren inferior, espalda,
     multiestación…), estado e imagen.
  3. Cambia su estado (disponible o en mantenimiento), la edita o la elimina.
- **Alternos:**
  - 2a. Se elige una cadena en lugar de una sucursal: «Las máquinas solo se asignan a sucursales
    físicas, no a marcas».
- **Postcondición:** el cliente ve las máquinas en el detalle de la sucursal.
- **Origen:** `/machines/*`; MachineInventoryScreen, MachineFormModal (web).

### CU-19 Gestionar planes de suscripción
- **Actor:** gerente · **Canal:** web
- **Flujo básico:**
  1. Crea un plan: nombre, descripción, precio mensual, cantidad máxima de sedes con acceso y
     beneficios.
  2. Edita o desactiva planes.
- **Postcondición:** el plan queda disponible para vender a socios.
- **Corrección al migrar:** en el proyecto base la creación de planes no exige rol (RT-04).
- **Decisión de modelo (paquete 2.3):** en el proyecto base los planes son globales, no
  pertenecen a una cadena. Con el diseño multi-tenant cada cadena define sus propios planes; se
  confirma con el docente porque cambia la estructura del dato.
- **Origen:** `/subscriptions/plans`.

### CU-20 Registrar suscripción y pago de un socio
- **Actores:** recepcionista, gerente · **Canal:** web
- **Flujo básico:**
  1. Busca al socio.
  2. Le asigna un plan, su sucursal principal, fechas de inicio y fin, y si se renueva solo.
  3. Registra el pago: monto, fecha, método y estado.
- **Postcondición:** suscripción ACTIVA con su pago registrado.
- **Origen:** `/subscriptions/*`, `/subscriptions/:id/payments`.

### CU-21 Consultar mi plan
- **Actor:** cliente · **Canal:** app
- **Flujo básico:**
  1. Abre «Mi plan».
  2. Ve su plan, el estado de la suscripción, la fecha de vencimiento y sus pagos.
- **Origen:** `GET /subscriptions/user/:userId`; MiPlanScreen (app).

### CU-22 Reservar una clase
- **Actor:** cliente (y recepcionista para reservas presenciales) · **Canal:** app y web
- **Precondición:** el cliente tiene una suscripción activa.
- **Flujo básico:**
  1. Elige sucursal, actividad y fecha en la tira de días.
  2. Ve los horarios con cupos disponibles.
  3. Confirma la reserva.
  4. El sistema verifica cupo y duplicados, y guarda la reserva confirmada con un código QR.
- **Alternos:**
  - P1. Sin suscripción activa: no puede reservar (validado en la API, RT-04).
  - 4a. Cupo agotado: «Cupo agotado. Intenta con otro horario» (dos reservas simultáneas no
    pueden superar el cupo).
  - 4b. Ya tiene reserva en ese horario y fecha: «Ya tienes una reserva activa para este horario y fecha».
  - 1a. Reserva presencial: la recepcionista la registra para un socio en mostrador.
- **Postcondición:** reserva CONFIRMADA; el cliente la ve en «Mis reservas» con su QR.
- **Origen:** `POST /reservations`, `POST /reservations/walk-in`, `GET /reservations/me`;
  ScheduleSelection, ReservarHorario, ReservationSuccess, MisReservas (app); ReservasView (web).

### CU-23 Cancelar una reserva
- **Actores:** cliente (las suyas), personal (las de su alcance) · **Canal:** app y web
- **Flujo básico:**
  1. Abre la reserva y elige «Cancelar».
  2. El sistema la marca como cancelada con fecha y avisa al dueño.
- **Alternos:**
  - 1a. El cliente intenta cancelar una reserva ajena: «No puedes cancelar reservas ajenas».
  - 1b. El personal intenta cancelar una reserva de otra sucursal: se rechaza.
- **Postcondición:** el cupo se libera.
- **Origen:** `PUT /reservations/:id/cancel`, `PATCH /reservations/bulk-status`.

### CU-24 Confirmar el ingreso a una clase con QR
- **Actores:** recepcionista, instructor · **Canal:** web y app
- **Precondición:** el cliente tiene una reserva confirmada.
- **Flujo básico:**
  1. El cliente muestra el QR de su reserva.
  2. El personal lee o ingresa el código.
  3. El sistema valida que la reserva sea de esa sucursal y la marca como asistida.
- **Alternos:**
  - 3a. Código que no es de reserva: «Este código QR no corresponde a una reserva».
  - 3b. Reserva ya usada o cancelada: «La reserva ya se encuentra …».
  - 3c. Rol sin permiso: «Nivel jerárquico insuficiente para confirmar ingresos».
- **Dependencia:** la lectura con cámara usa una librería fuera del PDF (pendiente); se migra con
  ingreso manual del código.
- **Origen:** `GET /reservations/:id/qr-token`, `POST /reservations/check-in/token`,
  `PATCH /reservations/:id/check-in`; QrScannerModal (web), EscanerScreen (app).

### CU-25 Inscribirse en la lista de espera
- **Actores:** cliente, recepcionista · **Canal:** app y web
- **Flujo básico:**
  1. El cliente elige un horario sin cupo y se anota en la lista de espera.
  2. El sistema le asigna la siguiente posición (orden de llegada).
  3. Cuando se libera un cupo, el personal asigna el lugar al primero de la lista.
- **Alternos:**
  - 1a. El cliente sale de la lista.
- **Postcondición:** entrada en espera, o asignada con fecha.
- **Nota:** en el proyecto base la asignación es manual; se migra igual.
- **Origen:** `/waitlist/*`.

### CU-26 Registrar ingreso y salida de la sucursal
- **Actores:** recepcionista; sistema · **Canal:** web y app
- **Flujo básico:**
  1. El socio o el personal muestra el QR de su carnet virtual.
  2. La recepcionista lo lee o ingresa el código; el sistema muestra nombre, rol y sucursal para confirmar.
  3. Registra el ingreso, o la salida si ya estaba dentro.
- **Alternos:**
  - 2a. Código no válido: «Código QR no válido».
  - 2b. Rol que no puede registrar personal: «Tu rol no tiene permisos para escanear personal».
  - S1. El sistema cierra cada 30 minutos los ingresos con más de 4 horas sin salida.
- **Postcondición:** registro de acceso con hora de ingreso y de salida.
- **Origen:** `/checkins/*`, scheduler de salida automática; CarnetVirtual, EscanerScreen (app),
  AuditoriaView (web).

### CU-27 Consultar la auditoría de accesos
- **Actores:** gerente, recepcionista · **Canal:** web y app
- **Flujo básico:**
  1. Abre «Auditoría».
  2. Filtra por estado, sucursal y rango de fechas.
  3. Ve los ingresos y salidas con su método (QR o manual).
- **Postcondición:** ninguna (consulta).
- **Origen:** `GET /checkins`, `/checkins/gym/:gymId`, `/checkins/history`; AuditoriaView,
  AuditoriaDashboard (web), AuditoriaSucursal, AuditHistory (app).

### CU-28 Definir el plan nutricional de un cliente
- **Actores:** entrenador, nutricionista · **Canal:** app
- **Precondición:** el cliente es su asesorado.
- **Flujo básico:**
  1. Abre el perfil del cliente.
  2. Define calorías diarias, proteínas, carbohidratos y grasas, y el plan de comidas.
  3. El cliente lo ve en «Mi plan».
- **Alternos:**
  - 1a. Sin relación activa: «No tienes una relación activa con este cliente».
- **Postcondición:** plan nutricional vigente del cliente.
- **Origen:** `POST/GET /staff/clients/:clientId/plan`, `GET /staff/advisors/my-plan`;
  TrainerPlanScreen, PerfilAlumno (app).

### CU-29 Gestionar citas nutricionales
- **Actor:** nutricionista · **Canal:** app
- **Flujo básico:**
  1. Ve sus citas del día y las próximas (fecha, hora, tipo de cita y paciente).
  2. Cambia el estado de una cita (inicia en PENDIENTE) y agrega notas.
- **Origen:** `/staff/my-appointments`, `/staff/agenda/appointments`,
  `/staff/appointments/:id/status`; NutritionistDashboard (app).

### CU-30 Recibir y configurar notificaciones
- **Actores:** todos; super administrador y gerente envían; sistema · **Canal:** app y web
- **Flujo básico:**
  1. El usuario ve sus notificaciones y las marca como leídas.
  2. Configura qué tipos de aviso quiere recibir.
  3. El super administrador gestiona plantillas; el gerente envía avisos a usuarios de su alcance.
- **Alternos del sistema:**
  - S1. Cada 15 minutos avisa de las reservas que empiezan en unas 2 horas.
  - S2. Cada día a las 9:00 avisa a cada asesor de los alumnos que no entrenan según su meta.
  - S3. Al cancelar una reserva avisa a su dueño.
- **Dependencia:** las notificaciones push (Expo Push) están pendientes de aprobación; se migran
  como notificaciones dentro de la app y del panel.
- **Origen:** `/notifications/*`, schedulers de recordatorios e inactividad; AlertaBanner,
  AlertasConfig (app).

### CU-31 Consultar el panel de resumen
- **Actores:** super administrador, gerente, recepcionista · **Canal:** web y app
- **Flujo básico:**
  1. Abre «Resumen».
  2. Ve indicadores de su alcance: socios activos, ocupación, reservas, ingresos y actividad reciente.
- **Origen:** `GET /dashboard/summary`, `GET /gyms/me/dashboard-stats`; ResumenView (web),
  ManagerDashboard (app).

### CU-32 Generar reportes
- **Actores:** super administrador, gerente · **Canal:** web
- **Flujo básico:**
  1. Abre «Reportes» y elige uno.
  2. Aplica filtros (sucursal, fechas).
  3. Ve la vista previa con gráficos e indicadores, y la imprime.
- **Reportes del proyecto base:** asistencia, aforo, actividades, cancelaciones, máquinas
  (operación) · frecuencia, ranking, abandono (churn), auditoría, consolidado (gestión).
- **Dependencia:** la exportación a PDF usa una librería fuera del PDF (pendiente); se migra con
  vista imprimible.
- **Origen:** ReportesPage, ReportFilterModal, ReportPreviewModal y los 10 reportes (web).

### CU-33 Consultar sucursales y sus clases
- **Actor:** cliente · **Canal:** app
- **Flujo básico:**
  1. Abre «Inicio» o «Buscar».
  2. Ve las sucursales con su estado (abierta o cerrada) y su ocupación actual.
  3. Filtra por cadena o por actividad.
  4. Abre una sucursal y ve su dirección, horario de atención, clases con horarios y máquinas.
- **Alternos:**
  - 2a. Sucursales cercanas: la versión base las ordena por distancia con el GPS; depende del mapa
    y la geolocalización (pendiente de aprobación), así que se migra con la lista completa.
- **Postcondición:** ninguna (consulta); desde el detalle puede reservar (CU-22).
- **Origen:** `GET /gyms`, `/gyms/:id`, `/gyms/:id/schedules`, `/gyms/:id/occupancy`,
  `/activities`, `/machines`; Inicio, Buscar, SedesCatalog, SedeInfoModal, MachineCatalogList (app).

### CU-34 Consultar reportes del personal
- **Actores:** entrenador, instructor · **Canal:** app
- **Flujo básico:**
  1. Abre «Seguimiento» o su panel.
  2. El instructor ve la asistencia de sus clases; el entrenador ve la actividad reciente y el
     avance de sus alumnos.
  3. Abre la vista previa del reporte.
- **Postcondición:** ninguna (consulta).
- **Origen:** `GET /staff/me/stats/attendance`, `/staff/me/advisee-activity`,
  `/staff/dashboard-stats`; TrainerDashboard, InstructorReport, TrainerReport, ReportPreview (app).

---

## 5. Casos de uso de las mejoras (fase 2)

Los casos CU-01 a CU-34 describen el sistema migrado y sirven como **pruebas de regresión**. Los
casos CU-35 a CU-40 son las mejoras de la fase 2 ([../planificacion/README.md](../planificacion/README.md)).
Todos cumplen RT-01 a RT-05. Las excepciones comunes son las siguientes, y se informan sin exponer
datos sensibles:
- acceso fuera del alcance del usuario;
- información incompleta;
- servicio externo no disponible.

### CU-35 Revisar la técnica mediante video (RF-01)
- **Actores:** cliente, entrenador · **Canal:** app (envío y resultado) y web (revisión)
- **Precondición:** el cliente tiene una asesoría activa con el entrenador.
- **Flujo básico:**
  1. El cliente graba o elige el video de un ejercicio y lo envía.
  2. El sistema genera un análisis preliminar, que queda visible **solo para el entrenador**.
  3. El entrenador revisa el video y el análisis, y valida o corrige la valoración con su observación.
  4. El cliente recibe la devolución aprobada.
- **Alternos:** 2a. Sin tecnología de análisis aprobada, el entrenador evalúa el video directamente
  y el análisis preliminar se muestra como «no disponible».
- **Postcondición:** queda registrada la revisión, con la fecha, el entrenador y la valoración final.
- **Regla:** el análisis preliminar nunca reemplaza el criterio del entrenador.

### CU-36 Autorizar la portabilidad del perfil (RF-02)
- **Actores:** cliente, gerente de otra cadena · **Canal:** app (consentimiento) y web (consulta)
- **Flujo básico:**
  1. El cliente elige a qué cadena le permite consultar su perfil deportivo y qué datos comparte.
  2. El sistema registra el consentimiento, con su alcance y su fecha.
  3. El personal autorizado de esa cadena consulta solo los datos permitidos.
  4. El cliente puede revocar el consentimiento en cualquier momento.
- **Postcondición:** el consentimiento o la revocación quedan registrados y son verificables.
- **Regla:** sin consentimiento vigente, otra cadena no accede al historial (RT-01).

### CU-37 Obtener y canjear recompensas (RF-03)
- **Actores:** cliente, recepcionista, gerente · **Canal:** app (consulta y canje) y web (gestión)
- **Flujo básico:**
  1. El gerente define beneficios y cupones, con su condición, vigencia y límite de uso.
  2. El cliente ve los beneficios para los que es elegible.
  3. Pide el canje, y el sistema verifica la elegibilidad, la vigencia y el uso.
  4. Se registra el canje y se muestra el comprobante a recepción.
- **Alternos:** 3a. El cupón está vencido, agotado o ya fue usado: se rechaza el canje con el motivo.
- **Postcondición:** el canje queda registrado una sola vez, sin duplicados.

### CU-38 Gestionar alertas de inactividad (RF-04)
- **Actores:** entrenador, gerente, sistema · **Canal:** web
- **Flujo básico:**
  1. El gerente configura los umbrales de inactividad de su cadena.
  2. El sistema detecta a diario a los socios que superan el umbral.
  3. El entrenador o el gerente ven la alerta y registran el contacto o el seguimiento.
- **Postcondición:** la intervención queda registrada en la alerta.
- **Regla:** las alertas respetan el alcance de la cadena y de la sucursal (RT-01, RT-02).

### CU-39 Renovar la membresía con QR (RF-05)
- **Actores:** cliente, recepcionista · **Canal:** app (pago) y web (confirmación)
- **Flujo básico:**
  1. El cliente pide renovar su plan y el sistema genera una orden con su QR.
  2. El cliente paga y adjunta o registra el comprobante.
  3. Recepción confirma el pago y la membresía se renueva.
  4. El cliente consulta el estado de la orden: pendiente o confirmado.
- **Postcondición:** la orden, el comprobante y el estado quedan registrados.
- **Regla:** sin confirmación, la membresía no se activa automáticamente.

### CU-40 Consultar simulaciones de ejercicios y nutrición (RF-06)
- **Actores:** cliente, entrenador, nutricionista · **Canal:** app (consulta) y web (contenido)
- **Flujo básico:**
  1. El cliente abre un ejercicio de su rutina y ve su simulación o animación.
  2. Ve las pautas nutricionales asociadas a su plan asignado.
- **Postcondición:** ninguna (consulta).
- **Regla:** el contenido nutricional pertenece al plan que asignó el profesional; no son sugerencias
  clínicas automáticas.
