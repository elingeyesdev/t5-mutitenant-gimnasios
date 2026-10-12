# Historias de usuario · Gestión Gimnasios (migración)

Paquete EDT **2.2**. Cada historia sale de un caso de uso de [casos-de-uso.md](casos-de-uso.md) y
cabe en un paquete de la EDT ([../planificacion/README.md](../planificacion/README.md)). Las
historias describen lo que el proyecto base ya hace; los criterios de aceptación son los que se
usan para verificar que se migró bien.

## Cómo leerlas

- **Producto:** Web (`web/`, Jean) o Móvil (`movil/`, Melani). Cuando una historia móvil necesita
  API, la API se construye en el paquete web indicado («API en»).
- **Puntos:** tamaño relativo (1, 2, 3, 5, 8). **Prioridad:** Alta si está en la ruta crítica o
  bloquea a otra historia.
- **RT-xx:** reglas transversales de casos-de-uso.md §3. Toda historia web cumple RT-01 a RT-04
  aunque no se repitan en sus criterios.

**Una buena historia cumple INVEST:** independiente, negociable, valiosa, estimable, pequeña
(cabe en una iteración) y testeable (tiene criterios de prueba).

**Definición de «Listo»** (no se programa una historia sin): caso de uso aprobado por el docente,
criterios de aceptación escritos, mockup de la pantalla (paquetes 2.4 y 2.5), estimación PERT y
fechas en Planner.

---

## Web · panel de administración y API

### HU-01 · Iniciar sesión en el panel
**Web · 3 pts · Prioridad alta** — Viene de: CU-01 · EDT 3.1

Como personal administrativo quiero iniciar sesión en el panel para gestionar mi cadena o mi sucursal.

- [ ] Dado un usuario activo, cuando ingresa correo y contraseña correctos, entonces entra al panel y ve solo el menú que permite su rol.
- [ ] Dado un correo o contraseña incorrectos, cuando intenta entrar, entonces ve «Credenciales inválidas» sin indicar cuál dato falló.
- [ ] Dado un usuario desactivado, cuando intenta entrar, entonces ve «Cuenta desactivada».
- [ ] Dado más de 10 intentos en un minuto, cuando intenta otra vez, entonces el sistema lo rechaza temporalmente.
- [ ] Dado que la app móvil envía credenciales válidas a la API, cuando inicia sesión, entonces recibe un token con su rol, cadena y sucursal.

### HU-02 · Registrarse como cliente desde la web
**Web · 2 pts · Prioridad baja** — Viene de: CU-02 · EDT 3.1

Como visitante quiero crear mi cuenta de cliente desde la web para empezar a usar el gimnasio.

- [ ] Dado un correo y carnet no registrados, cuando completa el formulario, entonces se crea una cuenta con rol Cliente.
- [ ] Dado un correo ya registrado, cuando envía el formulario, entonces ve «Ya existe un usuario con este email».
- [ ] Dado un carnet ya registrado, cuando envía el formulario, entonces ve que el CI ya está en otra cuenta.

### HU-03 · Gestionar roles, permisos y parámetros
**Web · 3 pts · Prioridad media** — Viene de: CU-04 · EDT 3.1

Como super administrador quiero administrar los roles, sus permisos y los parámetros del sistema para controlar qué puede hacer cada usuario.

- [ ] Dado la vista «Roles», cuando la abre, entonces ve los 9 roles con su nivel jerárquico y sus permisos.
- [ ] Dado un rol, cuando le asigna un permiso o asigna el rol a un usuario, entonces el cambio rige en el siguiente inicio de sesión de ese usuario.
- [ ] Dado un rol de sistema, cuando intenta eliminarlo, entonces el sistema lo impide.
- [ ] Dado un usuario que no es super administrador, cuando llama a estas funciones por la API, entonces recibe acceso denegado.

### HU-04 · Registrar cadenas y sucursales
**Web · 5 pts · Prioridad alta** — Viene de: CU-05 · EDT 3.2

Como super administrador quiero registrar cadenas y sus sucursales para que cada gimnasio opere de forma independiente.

- [ ] Dado el formulario de cadena, cuando registra una nueva, entonces queda creada como inquilino sin sucursales.
- [ ] Dado una cadena, cuando registra una sucursal con nombre, capacidad, ubicación y estado, entonces la sucursal pertenece a esa cadena.
- [ ] Dado dos cadenas A y B, cuando el gerente de A consulta sucursales, usuarios o cualquier dato, entonces no ve nada de B (RT-01).
- [ ] Dado el gerente de una cadena, cuando intenta crear o eliminar una cadena o sucursal, entonces el sistema lo impide.

### HU-05 · Configurar mis sucursales
**Web · 3 pts · Prioridad alta** — Viene de: CU-05 · EDT 3.2

Como gerente quiero editar los datos, la ubicación y el horario de atención de mis sucursales para que los socios vean información correcta.

- [ ] Dado una sucursal de su cadena, cuando edita nombre, capacidad, estado o ubicación, entonces se guardan los cambios.
- [ ] Dado el horario de atención, cuando define apertura y cierre por día, entonces se muestra en el detalle de la sucursal.
- [ ] Dado una sucursal de otra cadena, cuando intenta editarla, entonces ve «No tienes jurisdicción sobre esta sucursal».
- [ ] Dado una sucursal, cuando consulta su ocupación, entonces ve cuántas personas hay dentro respecto de su capacidad.

### HU-06 · Gestionar usuarios de mi alcance
**Web · 5 pts · Prioridad alta** — Viene de: CU-06 · EDT 3.3

Como gerente quiero crear, editar y dar de baja a los usuarios de mi cadena para mantener al día al personal y a los socios.

- [ ] Dado la vista «Usuarios», cuando la abre, entonces ve la lista paginada solo de su alcance: el super administrador todos, el gerente su cadena, la recepcionista su sucursal.
- [ ] Dado un nuevo usuario, cuando lo crea con rol y sucursal de su cadena, entonces queda activo y asociado a esa cadena.
- [ ] Dado un rol de nivel igual o superior al propio, cuando intenta asignarlo, crear o eliminar a ese usuario, entonces el sistema lo impide (RT-03).
- [ ] Dado su propia cuenta, cuando intenta eliminarla, entonces ve «Medida de seguridad: No puedes eliminar tu propia cuenta».
- [ ] Dado un correo o carnet ya registrado, cuando guarda, entonces el sistema lo rechaza.

### HU-07 · Registrar socios en mi sucursal
**Web · 3 pts · Prioridad alta** — Viene de: CU-06 · EDT 3.3

Como recepcionista quiero registrar socios en mi sucursal para atenderlos en mostrador.

- [ ] Dado el formulario de socio, cuando lo registra, entonces el socio queda como Cliente con membresía en la cadena y su sucursal habitual.
- [ ] Dado una persona que ya tiene cuenta en otra cadena, cuando se registra en esta, entonces se crea la membresía sin duplicar la persona ni copiar su historial a la otra cadena (diseño de contexto-proyecto.md §3.2).
- [ ] Dado una recepcionista, cuando intenta registrar o editar usuarios de otra sucursal, entonces el sistema lo impide (RT-02).

### HU-08 · Asignar horarios al personal
**Web · 3 pts · Prioridad media** — Viene de: CU-08 · EDT 3.4

Como gerente quiero asignar el horario semanal a cada miembro del personal para organizar la atención en la sucursal.

- [ ] Dado un miembro del personal de su alcance, cuando le asigna día, hora de inicio y fin, entonces el horario queda guardado.
- [ ] Dado el horario asignado, cuando el personal lo consulta desde la app, entonces ve su semana.
- [ ] Dado personal de otra cadena, cuando intenta asignarle horario, entonces el sistema lo impide.

### HU-09 · Gestionar el catálogo de ejercicios
**Web · 3 pts · Prioridad alta** — Viene de: CU-11 · EDT 3.5

Como super administrador quiero mantener el catálogo de ejercicios para que los entrenadores armen rutinas con ejercicios estándar.

- [ ] Dado la biblioteca, cuando crea o edita un ejercicio con sus grupos musculares, categoría, dificultad, equipo, video de YouTube e imagen, entonces queda disponible para todas las cadenas.
- [ ] Dado la biblioteca, cuando busca o filtra por grupo muscular, entonces ve solo los ejercicios que coinciden.
- [ ] Dado un usuario que no es super administrador, cuando intenta crear o editar ejercicios, entonces el sistema lo impide.
- [ ] Dado los datos de prueba, cuando se crea la base, entonces el catálogo trae los 85 ejercicios del proyecto base.

### HU-10 · Gestionar rutinas desde el panel
**Web · 3 pts · Prioridad media** — Viene de: CU-12 · EDT 3.5

Como entrenador quiero crear y asignar rutinas desde el panel para preparar el plan de mis alumnos en una pantalla grande.

- [ ] Dado un alumno con asesoría activa, cuando crea una rutina con ejercicios, series, repeticiones, peso, descanso y día, entonces queda asignada al alumno.
- [ ] Dado una rutina, cuando la guarda como plantilla, entonces puede reutilizarla con otro alumno.
- [ ] Dado una rutina de su sucursal, cuando intenta moverla a otra sucursal, entonces ve «No puede mover la rutina a otra sucursal».

### HU-11 · Gestionar actividades y horarios de clases
**Web · 5 pts · Prioridad alta** — Viene de: CU-16 · EDT 3.7

Como gerente quiero crear actividades y sus horarios con un instructor para que los socios puedan reservar clases.

- [ ] Dado una sucursal de su cadena, cuando crea una actividad con nombre, duración y tipo de acceso, entonces queda en esa sucursal.
- [ ] Dado una actividad, cuando agrega un horario con día, hora, cupo máximo e instructor, entonces solo puede elegir instructores activos de esa sucursal.
- [ ] Dado que no eligió sucursal, cuando guarda, entonces ve «Debes seleccionar una sucursal para el servicio».
- [ ] Dado una recepcionista, cuando gestiona actividades, entonces solo ve y modifica las de su sucursal.

### HU-12 · Gestionar el inventario de máquinas
**Web · 3 pts · Prioridad media** — Viene de: CU-18 · EDT 3.8

Como gerente quiero registrar las máquinas de cada sucursal y su estado para saber qué equipos están disponibles.

- [ ] Dado una sucursal, cuando registra una máquina con nombre, categoría, estado e imagen, entonces aparece en su inventario.
- [ ] Dado una máquina, cuando cambia su estado a mantenimiento o disponible, entonces el cambio se ve en el detalle de la sucursal en la app.
- [ ] Dado una cadena en lugar de una sucursal, cuando intenta asignarle una máquina, entonces ve «Las máquinas solo se asignan a sucursales físicas, no a marcas».

### HU-13 · Gestionar planes de suscripción
**Web · 2 pts · Prioridad alta** — Viene de: CU-19 · EDT 3.9

Como gerente quiero definir los planes de suscripción para vender membresías a los socios.

- [ ] Dado el formulario de plan, cuando lo crea con nombre, precio mensual, sedes con acceso y beneficios, entonces queda disponible para asignar.
- [ ] Dado un usuario sin rol administrativo, cuando intenta crear o editar un plan por la API, entonces recibe acceso denegado (corrección RT-04).
- [ ] Dado los datos de prueba, cuando se crea la base, entonces hay planes de ejemplo (el proyecto base no los tenía y sin ellos nadie podía reservar).

### HU-14 · Registrar suscripción y pago de un socio
**Web · 3 pts · Prioridad alta** — Viene de: CU-20 · EDT 3.9

Como recepcionista quiero asignar un plan a un socio y registrar su pago para que pueda usar el gimnasio.

- [ ] Dado un socio, cuando le asigna un plan con sucursal principal y fechas, entonces su suscripción queda ACTIVA.
- [ ] Dado una suscripción, cuando registra un pago con monto, fecha y método, entonces aparece en el historial de pagos.
- [ ] Dado una suscripción vencida, cuando se consulta, entonces no figura como activa.

### HU-15 · Gestionar las reservas de mi sucursal
**Web · 5 pts · Prioridad alta** — Viene de: CU-22, CU-23 · EDT 3.10

Como recepcionista quiero ver, registrar y cancelar reservas de mi sucursal para controlar los cupos de cada clase.

- [ ] Dado la vista «Reservas», cuando la abre, entonces ve las reservas de su alcance filtrables por fecha y estado.
- [ ] Dado un socio en mostrador, cuando registra una reserva presencial, entonces se descuenta un cupo del horario.
- [ ] Dado un horario lleno, cuando se intenta reservar, entonces el sistema responde «Cupo agotado. Intenta con otro horario», incluso si dos reservas llegan al mismo tiempo.
- [ ] Dado un socio sin suscripción activa, cuando se intenta reservar por la API, entonces el sistema lo rechaza (corrección RT-04).
- [ ] Dado varias reservas, cuando cambia su estado en bloque, entonces todas se actualizan.

### HU-16 · Confirmar el ingreso a una clase con el código de la reserva
**Web · 3 pts · Prioridad alta** — Viene de: CU-24 · EDT 3.10

Como recepcionista quiero validar el código de la reserva de un socio para confirmar que asistió a la clase.

- [ ] Dado una reserva confirmada de su sucursal, cuando ingresa su código, entonces la reserva queda como asistida.
- [ ] Dado un código que no es de reserva, cuando lo valida, entonces ve «Este código QR no corresponde a una reserva».
- [ ] Dado una reserva ya usada o cancelada, cuando la valida, entonces ve «La reserva ya se encuentra …».

### HU-17 · Asignar cupos de la lista de espera
**Web · 2 pts · Prioridad media** — Viene de: CU-25 · EDT 3.11

Como recepcionista quiero asignar un cupo liberado al primero de la lista de espera para respetar el orden de llegada.

- [ ] Dado un horario con lista de espera, cuando la consulta, entonces ve a los socios en orden de llegada.
- [ ] Dado un cupo liberado, cuando asigna el lugar al primero, entonces su entrada pasa a ASIGNADA con fecha.

### HU-18 · Registrar ingreso y salida de la sucursal
**Web · 3 pts · Prioridad alta** — Viene de: CU-26 · EDT 3.12

Como recepcionista quiero registrar el ingreso y la salida de socios y personal para controlar quién está en la sucursal.

- [ ] Dado el código del carnet virtual, cuando lo ingresa, entonces ve nombre, rol y sucursal antes de confirmar.
- [ ] Dado una persona fuera de la sucursal, cuando confirma, entonces se registra su ingreso; si ya estaba dentro, se registra su salida.
- [ ] Dado un código no válido, cuando lo ingresa, entonces ve «Código QR no válido».
- [ ] Dado un ingreso sin salida por más de 4 horas, cuando corre la tarea programada (cada 30 minutos), entonces se registra la salida automáticamente.

### HU-19 · Consultar la auditoría de accesos
**Web · 3 pts · Prioridad media** — Viene de: CU-27 · EDT 3.12

Como gerente quiero revisar los ingresos y salidas de mis sucursales para controlar la asistencia.

- [ ] Dado la vista «Auditoría», cuando filtra por estado, sucursal y rango de fechas, entonces ve solo los registros que coinciden.
- [ ] Dado un registro, cuando lo consulta, entonces ve hora de ingreso, hora de salida y método (QR o manual).
- [ ] Dado un gerente, cuando consulta, entonces solo ve las sucursales de su cadena.

### HU-20 · Enviar avisos y gestionar plantillas
**Web · 3 pts · Prioridad baja** — Viene de: CU-30 · EDT 3.14

Como gerente quiero enviar avisos a los usuarios de mi cadena para comunicar novedades.

- [ ] Dado una plantilla, cuando el super administrador la crea, entonces el gerente puede usarla al enviar avisos.
- [ ] Dado un aviso, cuando lo envía, entonces llega solo a usuarios de su alcance y aparece en sus notificaciones.
- [ ] Dado una reserva que empieza en unas 2 horas, cuando corre la tarea programada (cada 15 minutos), entonces su dueño recibe un recordatorio una sola vez.
- [ ] Dado un alumno que no entrena según su meta, cuando corre la tarea diaria de las 9:00, entonces su asesor recibe una alerta de inactividad.

### HU-21 · Consultar el panel de resumen
**Web · 3 pts · Prioridad media** — Viene de: CU-31 · EDT 3.15

Como gerente quiero ver los indicadores principales de mi cadena para tomar decisiones rápido.

- [ ] Dado el panel «Resumen», cuando lo abre, entonces ve socios activos, ocupación, reservas e ingresos de su alcance.
- [ ] Dado un super administrador, cuando abre el panel, entonces ve los totales de todas las cadenas.
- [ ] Dado los mismos datos de prueba, cuando se compara con el proyecto base, entonces los indicadores coinciden.

### HU-22 · Generar reportes de operación
**Web · 5 pts · Prioridad media** — Viene de: CU-32 · EDT 3.16

Como gerente quiero los reportes de asistencia, aforo, actividades, cancelaciones y máquinas para evaluar la operación de mis sucursales.

- [ ] Dado un reporte, cuando aplica filtros de sucursal y fechas, entonces ve la vista previa con sus gráficos e indicadores.
- [ ] Dado la vista previa, cuando la imprime desde el navegador, entonces el reporte sale legible en hoja carta.
- [ ] Dado los mismos datos de prueba, cuando se compara con el proyecto base, entonces los totales coinciden.

### HU-23 · Generar reportes de gestión
**Web · 5 pts · Prioridad media** — Viene de: CU-32 · EDT 3.17

Como gerente quiero los reportes de frecuencia, ranking, abandono, auditoría y consolidado para entender la retención de socios.

- [ ] Dado un reporte, cuando aplica filtros de sucursal y fechas, entonces ve la vista previa con sus gráficos e indicadores.
- [ ] Dado el reporte consolidado, cuando lo abre, entonces resume todos los indicadores del periodo.
- [ ] Dado los mismos datos de prueba, cuando se compara con el proyecto base, entonces los totales coinciden.

---

## Móvil · app del socio y del personal

### HU-24 · Iniciar sesión en la app
**Móvil · 3 pts · Prioridad alta** — Viene de: CU-01 · EDT 4.1 · API en 3.1

Como usuario quiero iniciar sesión en la app para ver las funciones de mi rol.

- [ ] Dado credenciales válidas, cuando inicia sesión, entonces ve el inicio de su rol: cliente, entrenador, instructor, nutricionista o gerente.
- [ ] Dado una sesión abierta, cuando cierra y vuelve a abrir la app, entonces sigue con la sesión iniciada.
- [ ] Dado credenciales inválidas, cuando intenta entrar, entonces ve el mensaje de error sin cerrar la app.
- [ ] Dado una sesión vencida, cuando la app recibe «no autorizado» de la API, entonces vuelve al inicio de sesión.

### HU-25 · Registrarme desde la app
**Móvil · 2 pts · Prioridad alta** — Viene de: CU-02 · EDT 4.2 · API en 3.1

Como visitante quiero crear mi cuenta de cliente desde la app para empezar a reservar.

- [ ] Dado datos válidos, cuando completa el registro, entonces entra a la app como cliente.
- [ ] Dado un correo o carnet ya registrado, cuando envía el formulario, entonces ve el mensaje de la API.

### HU-26 · Recuperar mi contraseña
**Móvil · 2 pts · Prioridad baja** — Viene de: CU-03 · EDT 4.2 · API en 3.1
**Bloqueada:** requiere aprobación del docente para el envío de correo.

Como usuario quiero recuperar mi contraseña con un código enviado a mi correo para volver a entrar si la olvido.

- [ ] Dado su correo registrado, cuando solicita recuperar, entonces recibe un código de 6 dígitos con vencimiento.
- [ ] Dado el código correcto, cuando define una nueva contraseña, entonces puede iniciar sesión con ella.
- [ ] Dado un código inválido o vencido, cuando lo ingresa, entonces ve «Código OTP inválido».

### HU-27 · Actualizar mi perfil y mi perfil de entrenamiento
**Móvil · 3 pts · Prioridad media** — Viene de: CU-07 · EDT 4.2 · API en 3.3 y 3.6

Como cliente quiero completar mis datos, contactos de emergencia y perfil de entrenamiento para que mi asesor me conozca.

- [ ] Dado «Mis datos personales», cuando edita teléfono, fecha de nacimiento, género, altura y foto, entonces se guardan los cambios.
- [ ] Dado contactos de emergencia, cuando agrega uno, entonces aparece en su perfil.
- [ ] Dado su perfil de entrenamiento, cuando registra objetivo, preferencias y restricciones médicas, entonces su asesor las ve en su ficha.

### HU-28 · Ejecutar mi rutina y registrar mis series
**Móvil · 5 pts · Prioridad alta** — Viene de: CU-13 · EDT 4.3 · API en 3.5 y 3.6

Como cliente quiero seguir mi rutina en la app y anotar cada serie para entrenar guiado y guardar lo que hice.

- [ ] Dado una rutina asignada, cuando inicia el entrenamiento, entonces ve los ejercicios del día con su video.
- [ ] Dado un ejercicio, cuando registra repeticiones y peso de una serie, entonces la app inicia el descanso indicado.
- [ ] Dado el último ejercicio, cuando finaliza, entonces ve el resumen y la sesión queda en su historial.
- [ ] Dado que no tiene rutina, cuando elige entrenamiento libre, entonces puede elegir ejercicios del catálogo.

### HU-29 · Ver mi historial de entrenamientos
**Móvil · 3 pts · Prioridad media** — Viene de: CU-15 · EDT 4.4 · API en 3.6

Como cliente quiero ver mis entrenamientos anteriores y mis récords para saber si estoy progresando.

- [ ] Dado sesiones registradas, cuando abre el historial, entonces las ve por fecha con sus ejercicios y series.
- [ ] Dado sus sesiones, cuando abre «Récords», entonces ve el mayor peso logrado por ejercicio.

### HU-30 · Registrar mis medidas y ver mi evolución
**Móvil · 3 pts · Prioridad media** — Viene de: CU-14, CU-15 · EDT 4.5 · API en 3.6

Como cliente quiero registrar mis medidas y ver su evolución para medir mis resultados.

- [ ] Dado el formulario de medidas, cuando registra peso, grasa, masa muscular y circunferencias, entonces se guarda con la fecha.
- [ ] Dado su peso y altura, cuando ve sus medidas, entonces la app muestra su IMC con la categoría.
- [ ] Dado varios registros, cuando abre «Mi evolución física», entonces ve la línea de tiempo de cada medida.
- [ ] Dado sus objetivos, cuando los actualiza, entonces se reflejan en su cuadro de mando.

### HU-31 · Solicitar un asesor
**Móvil · 2 pts · Prioridad media** — Viene de: CU-09 · EDT 4.6 · API en 3.4

Como cliente quiero elegir un entrenador o nutricionista de mi sucursal para recibir un plan personalizado.

- [ ] Dado el catálogo de personal, cuando lo abre, entonces ve los entrenadores y nutricionistas de su sucursal.
- [ ] Dado un asesor, cuando envía la solicitud, entonces queda pendiente y la ve en «Mis solicitudes».
- [ ] Dado una asesoría pendiente o activa, cuando la cancela, entonces queda cancelada.

### HU-32 · Responder solicitudes y ver mis alumnos
**Móvil · 3 pts · Prioridad media** — Viene de: CU-10, CU-34 · EDT 4.6 · API en 3.4

Como entrenador quiero aceptar o rechazar solicitudes y ver a mis alumnos para organizar mi trabajo.

- [ ] Dado solicitudes pendientes, cuando acepta una, entonces el cliente aparece en «Mis alumnos».
- [ ] Dado una solicitud dirigida a otro asesor, cuando intenta aceptarla, entonces ve «Solo el asesor destinatario puede aceptar esta solicitud».
- [ ] Dado un alumno, cuando fija su meta de sesiones por semana, entonces esa meta se usa para la alerta de inactividad.
- [ ] Dado su panel, cuando lo abre, entonces ve la actividad reciente de sus alumnos.

### HU-33 · Asignar una rutina desde la app
**Móvil · 3 pts · Prioridad media** — Viene de: CU-12 · EDT 4.6 · API en 3.5

Como entrenador quiero armar y asignar la rutina de un alumno desde la app para hacerlo desde la sala.

- [ ] Dado un alumno, cuando arma una rutina con ejercicios del catálogo y la asigna, entonces el alumno la ve en «Mi rutina».
- [ ] Dado la ficha del alumno, cuando la abre, entonces ve sus datos, restricciones médicas y últimas sesiones.

### HU-34 · Ver el inicio y las sucursales
**Móvil · 3 pts · Prioridad alta** — Viene de: CU-33 · EDT 4.7 · API en 3.2, 3.7 y 3.8

Como cliente quiero ver las sucursales con sus clases y máquinas para decidir dónde entrenar.

- [ ] Dado el inicio, cuando lo abre, entonces ve su saludo, accesos a entrenamiento, historial y progreso.
- [ ] Dado «Buscar», cuando filtra por cadena o actividad, entonces ve las sucursales que coinciden con su ocupación y estado.
- [ ] Dado una sucursal, cuando abre su detalle, entonces ve dirección, horario de atención, clases con horarios y máquinas con su estado.

### HU-35 · Reservar una clase
**Móvil · 5 pts · Prioridad alta** — Viene de: CU-22 · EDT 4.8 · API en 3.10

Como cliente quiero reservar un horario de clase desde la app para asegurar mi lugar.

- [ ] Dado una suscripción activa, cuando elige actividad, fecha y horario con cupo, entonces la reserva se confirma y ve su QR.
- [ ] Dado que no tiene suscripción activa, cuando intenta reservar, entonces la app le avisa y no permite continuar.
- [ ] Dado un horario lleno, cuando intenta reservar, entonces ve «Cupo agotado. Intenta con otro horario».
- [ ] Dado una reserva activa en el mismo horario y fecha, cuando intenta reservar otra vez, entonces ve que ya tiene esa reserva.

### HU-36 · Ver y cancelar mis reservas
**Móvil · 3 pts · Prioridad alta** — Viene de: CU-23, CU-24 · EDT 4.8 · API en 3.10

Como cliente quiero ver mis reservas con su código QR y cancelar las que no usaré para liberar el cupo.

- [ ] Dado sus reservas, cuando abre «Mis reservas», entonces las ve separadas en próximas y pasadas con su estado.
- [ ] Dado una reserva confirmada, cuando la abre, entonces ve el QR que la recepción valida.
- [ ] Dado una reserva próxima, cuando la cancela, entonces pasa a cancelada y el cupo se libera.

### HU-37 · Anotarme en la lista de espera
**Móvil · 2 pts · Prioridad media** — Viene de: CU-25 · EDT 4.8 · API en 3.11

Como cliente quiero anotarme en la lista de espera de un horario lleno para entrar si se libera un cupo.

- [ ] Dado un horario sin cupo, cuando elige «Lista de espera», entonces queda anotado y ve su posición.
- [ ] Dado que está en la lista, cuando sale de ella, entonces su lugar se libera.

### HU-38 · Mostrar mi carnet virtual
**Móvil · 2 pts · Prioridad alta** — Viene de: CU-26 · EDT 4.8 · API en 3.12

Como socio quiero mostrar mi carnet virtual con QR para ingresar a la sucursal.

- [ ] Dado su cuenta, cuando abre «Carnet virtual», entonces ve su nombre, foto, sucursal y un código QR.
- [ ] Dado su carnet, cuando la recepción lo lee o ingresa el código, entonces se registra su ingreso.

### HU-39 · Ver mi plan y mi plan nutricional
**Móvil · 2 pts · Prioridad media** — Viene de: CU-21, CU-28 · EDT 4.9 · API en 3.9 y 3.13

Como cliente quiero ver mi suscripción y el plan nutricional que me dio mi asesor para seguirlo.

- [ ] Dado su suscripción, cuando abre «Mi plan», entonces ve el plan, su estado, el vencimiento y sus pagos.
- [ ] Dado un plan nutricional asignado, cuando lo abre, entonces ve calorías diarias, macronutrientes y el plan de comidas.

### HU-40 · Definir el plan nutricional de un asesorado
**Móvil · 3 pts · Prioridad media** — Viene de: CU-28 · EDT 4.9 · API en 3.13

Como nutricionista o entrenador quiero definir el plan nutricional de mi asesorado para guiar su alimentación.

- [ ] Dado un asesorado activo, cuando define calorías, proteínas, carbohidratos, grasas y comidas, entonces el plan queda vigente.
- [ ] Dado un cliente sin relación activa, cuando intenta definir su plan, entonces ve «No tienes una relación activa con este cliente».

### HU-41 · Ver mi agenda y registrar asistencia
**Móvil · 3 pts · Prioridad media** — Viene de: CU-17, CU-34 · EDT 4.10 · API en 3.7 y 3.15

Como instructor quiero ver mis clases del día y marcar la asistencia para llevar el control de cada clase.

- [ ] Dado su agenda, cuando la abre, entonces ve las clases que dicta hoy con su cantidad de reservas.
- [ ] Dado una clase, cuando marca la asistencia de cada participante, entonces queda registrada.
- [ ] Dado su panel, cuando abre sus estadísticas, entonces ve la asistencia de sus clases.

### HU-42 · Gestionar mis citas nutricionales
**Móvil · 2 pts · Prioridad baja** — Viene de: CU-29 · EDT 4.10 · API en 3.13

Como nutricionista quiero ver mis citas y actualizar su estado para organizar mis consultas.

- [ ] Dado sus citas, cuando abre su panel, entonces ve las de hoy y las próximas con paciente, hora y tipo.
- [ ] Dado una cita, cuando cambia su estado y agrega notas, entonces se guardan los cambios.

### HU-43 · Registrar ingresos desde la app
**Móvil · 3 pts · Prioridad media** — Viene de: CU-24, CU-26 · EDT 4.11 · API en 3.10 y 3.12

Como recepcionista quiero registrar ingresos y validar reservas desde la app para atender sin estar en el mostrador.

- [ ] Dado el código de un carnet, cuando lo ingresa, entonces ve los datos de la persona y registra su ingreso o salida.
- [ ] Dado el código de una reserva, cuando lo ingresa, entonces la reserva queda como asistida.
- [ ] Dado su historial, cuando lo abre, entonces ve los últimos registros que hizo.

### HU-44 · Ver el resumen y la auditoría de mi sucursal
**Móvil · 3 pts · Prioridad media** — Viene de: CU-27, CU-31 · EDT 4.11 · API en 3.12 y 3.15

Como gerente quiero ver los indicadores y los accesos de mis sucursales desde la app para supervisar desde cualquier lugar.

- [ ] Dado su panel, cuando lo abre, entonces ve los indicadores de su cadena.
- [ ] Dado «Auditoría de sucursal», cuando filtra por fecha, entonces ve los ingresos y salidas.

### HU-45 · Recibir y configurar mis avisos
**Móvil · 2 pts · Prioridad baja** — Viene de: CU-30 · EDT 4.12 · API en 3.14

Como usuario quiero ver mis avisos en la app y elegir cuáles recibir para enterarme de lo importante.

- [ ] Dado avisos nuevos, cuando abre la app, entonces ve un aviso destacado y la lista de notificaciones.
- [ ] Dado una notificación, cuando la abre, entonces queda marcada como leída.
- [ ] Dado «Configurar alertas», cuando desactiva un tipo, entonces deja de recibirlo.

---

## Trazabilidad

Del caso de uso al issue, como en el PDF: **CU › HU › issue › tarea de Planner › rama**. El número
de issue se asigna al crearlo en GitHub; la rama usa ese número (`feature/<issue>-<descripcion>`).

| HU | Título corto | CU | EDT | Act. | Producto | Pts | Prioridad |
|---|---|---|---|---|---|---|---|
| HU-01 | Login del panel | CU-01 | 3.1 | W01 | Web | 3 | Alta |
| HU-02 | Registro web | CU-02 | 3.1 | W01 | Web | 2 | Baja |
| HU-03 | Roles y permisos | CU-04 | 3.1 | W01 | Web | 3 | Media |
| HU-04 | Cadenas y sucursales | CU-05 | 3.2 | W02 | Web | 5 | Alta |
| HU-05 | Configurar sucursales | CU-05 | 3.2 | W02 | Web | 3 | Alta |
| HU-06 | Usuarios | CU-06 | 3.3 | W03 | Web | 5 | Alta |
| HU-07 | Registro de socios | CU-06 | 3.3 | W03 | Web | 3 | Alta |
| HU-08 | Horarios del personal | CU-08 | 3.4 | W04 | Web | 3 | Media |
| HU-09 | Catálogo de ejercicios | CU-11 | 3.5 | W05 | Web | 3 | Alta |
| HU-10 | Rutinas en el panel | CU-12 | 3.5 | W05 | Web | 3 | Media |
| HU-11 | Actividades y horarios | CU-16 | 3.7 | W07 | Web | 5 | Alta |
| HU-12 | Máquinas | CU-18 | 3.8 | W08 | Web | 3 | Media |
| HU-13 | Planes de suscripción | CU-19 | 3.9 | W09 | Web | 2 | Alta |
| HU-14 | Suscripción y pago | CU-20 | 3.9 | W09 | Web | 3 | Alta |
| HU-15 | Reservas de la sucursal | CU-22, CU-23 | 3.10 | W10 | Web | 5 | Alta |
| HU-16 | Ingreso a clase | CU-24 | 3.10 | W10 | Web | 3 | Alta |
| HU-17 | Lista de espera | CU-25 | 3.11 | W11 | Web | 2 | Media |
| HU-18 | Ingreso y salida | CU-26 | 3.12 | W12 | Web | 3 | Alta |
| HU-19 | Auditoría | CU-27 | 3.12 | W12 | Web | 3 | Media |
| HU-20 | Avisos y plantillas | CU-30 | 3.14 | W14 | Web | 3 | Baja |
| HU-21 | Panel de resumen | CU-31 | 3.15 | W15 | Web | 3 | Media |
| HU-22 | Reportes de operación | CU-32 | 3.16 | W16 | Web | 5 | Media |
| HU-23 | Reportes de gestión | CU-32 | 3.17 | W17 | Web | 5 | Media |
| HU-24 | Login de la app | CU-01 | 4.1 | M01 | Móvil | 3 | Alta |
| HU-25 | Registro en la app | CU-02 | 4.2 | M02 | Móvil | 2 | Alta |
| HU-26 | Recuperar contraseña | CU-03 | 4.2 | M02 | Móvil | 2 | Baja |
| HU-27 | Perfil | CU-07 | 4.2 | M02 | Móvil | 3 | Media |
| HU-28 | Ejecutar rutina | CU-13 | 4.3 | M03 | Móvil | 5 | Alta |
| HU-29 | Historial de entrenamientos | CU-15 | 4.4 | M04 | Móvil | 3 | Media |
| HU-30 | Medidas y evolución | CU-14, CU-15 | 4.5 | M05 | Móvil | 3 | Media |
| HU-31 | Solicitar asesor | CU-09 | 4.6 | M06 | Móvil | 2 | Media |
| HU-32 | Mis alumnos | CU-10, CU-34 | 4.6 | M06 | Móvil | 3 | Media |
| HU-33 | Rutina desde la app | CU-12 | 4.6 | M06 | Móvil | 3 | Media |
| HU-34 | Inicio y sucursales | CU-33 | 4.7 | M07 | Móvil | 3 | Alta |
| HU-35 | Reservar clase | CU-22 | 4.8 | M08 | Móvil | 5 | Alta |
| HU-36 | Mis reservas | CU-23, CU-24 | 4.8 | M08 | Móvil | 3 | Alta |
| HU-37 | Lista de espera (app) | CU-25 | 4.8 | M08 | Móvil | 2 | Media |
| HU-38 | Carnet virtual | CU-26 | 4.8 | M08 | Móvil | 2 | Alta |
| HU-39 | Mi plan y nutrición | CU-21, CU-28 | 4.9 | M09 | Móvil | 2 | Media |
| HU-40 | Plan nutricional (asesor) | CU-28 | 4.9 | M09 | Móvil | 3 | Media |
| HU-41 | Agenda del instructor | CU-17, CU-34 | 4.10 | M10 | Móvil | 3 | Media |
| HU-42 | Citas nutricionales | CU-29 | 4.10 | M10 | Móvil | 2 | Baja |
| HU-43 | Ingresos desde la app | CU-24, CU-26 | 4.11 | M11 | Móvil | 3 | Media |
| HU-44 | Resumen y auditoría (app) | CU-27, CU-31 | 4.11 | M11 | Móvil | 3 | Media |
| HU-45 | Avisos en la app | CU-30 | 4.12 | M12 | Móvil | 2 | Baja |

**Paquetes sin historia propia.** Los paquetes de gestión, diseño, pruebas y despliegue (A a L)
se registran como issues de tipo `docs`, `chore` o `ci`. Los paquetes web 3.6 (entrenamiento y
métricas) y 3.13 (plan nutricional y citas) no tienen pantallas en el panel: son la API de las
historias HU-27 a HU-30, HU-39, HU-40 y HU-42, y se registran como un issue `web` por paquete que
enlaza esas historias.

**Cobertura.** Los 34 casos de uso tienen al menos una historia.
