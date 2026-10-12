# Migración del proyecto base al Stack B

Estado al 2026-10-11: **migración completa**. GymSync (NestJS + React + Expo) quedó reescrito en
ASP.NET Core 10 (panel MVC + Web API), .NET MAUI 10 (Android) y SQL Server con EF Core, con los
mismos datos de prueba y las mismas reglas de negocio. Desde aquí se trabajan las mejoras (fase 2).

Cómo levantarlo: [web/README.md](../web/README.md) y [movil/README.md](../movil/README.md).

## Qué se migró

| Proyecto base | Stack B | Dónde |
|---|---|---|
| Backend NestJS (41 tablas, 10 módulos) | Web API de ASP.NET Core, 41 entidades con EF Core y una migración inicial | `web/GestionGimnasios.Web/Controllers/Api`, `Services`, `Data` |
| Seeder (`npm run db:seed`) | `SeedData` con la misma secuencia de nombres: 10 cadenas, 30 sucursales, 206 usuarios, 85 ejercicios | `web/.../Data/Seed` |
| `gym-scope.ts` | Tenant = Cadena con filtro global de EF Core; alcance por sucursal en `Alcances` | `Data/AppDbContext.cs`, `Services/Comun.cs` |
| JWT + guards por nivel | Cookie para el panel, JWT para la app, políticas por nivel (RT-03: nadie gestiona roles de su nivel o superior) | `Infrastructure/` |
| Tareas `@nestjs/schedule` | `BackgroundService`: salida automática a las 4 h, recordatorio 2 h antes de clase, alerta de inactividad diaria | `Services/TareasProgramadas.cs` |
| Swagger | OpenAPI integrado en `/openapi/v1.json` | `Program.cs` |
| Panel web React | Panel MVC/Razor con la plantilla **Tabler** (menú según rol) | `web/.../Views` |
| App Expo | App .NET MAUI con pestañas según rol | `movil/GestionGimnasios.Movil` |

Correcciones hechas durante la migración (fallas del proyecto base, RT-04): un cliente ya no puede
crear planes, reservar exige suscripción activa y las reservas usan bloqueo en la base de datos
para no sobrepasar el cupo.

## Tecnologías del proyecto base que no están en el PDF

No se agregó ninguna tecnología fuera del PDF del curso. Mientras el docente no apruebe una
alternativa, cada función se resolvió con lo que trae .NET:

| Función | Proyecto base | Solución actual | Opción a consultar |
|---|---|---|---|
| Ingreso con QR | expo-camera + QR con JWT | Recepción escribe el número de socio (carnet) o el código de 8 caracteres de la reserva | ZXing.Net.Maui |
| Tiempo real (aforo, chat) | Socket.io | La app consulta cada 4 s mientras el chat está abierto; el aforo se actualiza al recargar | SignalR |
| Notificaciones push | Expo Push | Notificaciones dentro de la app y la web, con preferencias por tipo | Firebase Cloud Messaging |
| Fotos | Cloudinary | Archivos en `wwwroot/uploads` del servidor | Cloudinary |
| Reportes PDF | @react-pdf/renderer | Vista del reporte lista para imprimir o guardar como PDF desde el navegador | QuestPDF |
| Mapa de sedes | Leaflet | Enlace a OpenStreetMap con la ubicación | Leaflet |
| Correo (recuperar contraseña) | Nodemailer | `SmtpClient` de .NET; sin servidor de correo configurado, el código se escribe en el log | MailKit |
| Datos sin conexión | SQLite | Sin modo offline: la app requiere conexión | SQLite |

## Pantallas de la app por rol

| Rol | Pestañas | Desde "Más" |
|---|---|---|
| Cliente | Inicio, Gimnasios (catálogo, detalle, reservar, lista de espera), Reservas (código de ingreso, cancelar), Entrenar (rutina, entrenamiento con cronómetro y descanso) | Carnet virtual, plan y pagos, ingresos, progreso y récords, medidas e IMC, plan nutricional, asesores, objetivos/restricción/contactos, mensajes, notificaciones, perfil |
| Entrenador / nutricionista | Panel (solicitudes, alumnos e inactividad, ficha, asignar rutina, plan nutricional, meta semanal), Citas (solo nutricionista), Mensajes | Turnos, clases de hoy, notificaciones, perfil |
| Instructor / coordinador | Agenda (clases e inscritos, asistencia, estadísticas), Mensajes | Turnos, notificaciones, perfil |
| Recepcionista / gerente | Escáner (carnet y reservas), Resumen, Mensajes | Turnos, auditoría de accesos, notificaciones, perfil |
| Super administrador | Resumen, Mensajes | Notificaciones, perfil |

La administración completa (usuarios, sedes, actividades, máquinas, ejercicios, planes, roles,
avisos y los 10 reportes) está en el panel web.

## Cuentas de prueba

Todas con la contraseña `Aaron123*` (la misma del proyecto base).

| Rol | Correo | Sede |
|---|---|---|
| Super administrador | admin@gymsync.com | Todas |
| Gerente | ana.iglesias@gmail.com | Cadena Reyes |
| Recepcionista | pedro.sanz1@gmail.com | Reyes Equipetrol |
| Entrenador | carmen.cabrera1@gmail.com | Reyes Equipetrol |
| Instructor | alberto.cortes1@gmail.com | Sport Villa 1ro de Mayo |
| Cliente con plan activo | carlos.lopez4@gmail.com | Reyes Equipetrol |
| Cliente sin plan | adrian.alvarez3@gmail.com | Olimpia Equipetrol |

El seeder del proyecto base no crea nutricionistas; para probar las citas se crea uno desde el
panel (Usuarios › Nuevo usuario, rol Nutricionista).

## Pruebas realizadas (2026-10-11)

| Prueba | Resultado |
|---|---|
| Pruebas unitarias de reglas de negocio (`dotnet test`) | 20 de 20 |
| API recorrida por rol: alcance multi-tenant, jerarquía, reservas, accesos, asesorías, reportes | 68 de 68 |
| Panel web: todas las pantallas con cada rol, permisos y acciones | 116 de 116 |
| App en el emulador (Android 15) con el APK release | Sin errores en el flujo de abajo |

Flujo probado en la app, de punta a punta y entre roles:

1. El cliente inicia sesión, busca su gimnasio, reserva una clase y recibe el código de ingreso.
2. Registra un entrenamiento libre con series y descanso; aparece en su progreso y récords.
3. Solicita un entrenador; el entrenador la acepta, le asigna una rutina y le escribe por chat.
   La respuesta del cliente llega sola al chat del entrenador.
4. Recepción registra el ingreso con el número de socio y la asistencia con el código de la
   reserva (con aviso si la reserva es de otra fecha).
5. El cliente ve la rutina asignada, la clase como asistida y las notificaciones de cada paso.
6. La nutricionista acepta una solicitud, agenda una cita y arma el plan de comidas por día.
7. Instructor, gerente y super administrador ven su agenda o resumen según su alcance.

## Limitaciones conocidas

- La app no funciona sin conexión (ver tabla de tecnologías).
- El aforo no se actualiza en vivo; se refresca al deslizar hacia abajo.
- El APK de `movil/releases` está firmado con la clave de depuración; para publicarlo en una
  tienda hace falta una clave propia.
