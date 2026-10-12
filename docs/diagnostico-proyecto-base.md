# Diagnóstico del repo base (elingeyesdev/equipo03) — GymSync

Fuente: lectura de README, package.json, docker-compose, app.module, entidades y seeder. Fecha: 2026-10-08.
No se ejecutó ni compiló nada; todo es lectura estática.

## 1. Dónde está cada parte (el README de `main` no tiene el backend)
| Rama | Contenido | Último commit |
|---|---|---|
| `main` | Monorepo: `apps/web` (React), `apps/mobile` (Expo), `packages/core` | 2026-06-28 12:09 |
| `backend` | API NestJS (la más reciente) | 2026-06-28 11:52 "Configuracion del docker" |
| `backend2` | API NestJS, 2 commits distintos a `backend` | 2026-06-28 01:07 |
| `v4`, `2.1` | Ramas de trabajo de web/mobile ya mezcladas en `main` | — |

Remoto original desconectado para push (`DISABLED_NO_PUSH`). Clones locales:
- `referencia-equipo03/` (main)
- `referencia-backend/` (worktree de `origin/backend`)

## 2. Stack actual
| Capa | Tecnología |
|---|---|
| Web | React 19, Vite, TypeScript, Tailwind 4, React Query, Leaflet, recharts, @react-pdf/renderer, html5-qrcode, socket.io-client |
| Móvil | Expo 54, React Native 0.81, Zustand, React Navigation, expo-sqlite (cola offline), expo-camera (QR), expo-location, expo-notifications |
| Backend | NestJS 11, TypeORM 0.3, PostgreSQL 15, JWT+Passport, Socket.io, Swagger, helmet, throttler, schedule (cron), cache-manager |
| Externos | Cloudinary (archivos), Nodemailer (SMTP), Expo Push |
| Core compartido | `packages/core`: arquitectura hexagonal (entidades, puertos, casos de uso) usada por web y móvil |

## 3. Cómo se levanta según el README
1. Backend: `cp .env.example .env` → `npm install` → `npm run start:dev` (puerto 3000, prefijo `/api`, Swagger en `/api/docs`).
2. Seed (1ª vez): `npm run db:seed`.
3. Web: `.env` con `VITE_API_URL=http://localhost:3000` → `npm run dev` (5173).
4. Móvil: `.env` con `EXPO_PUBLIC_API_BASE_URL=http://IP:3000` → `npx expo start`.
5. `.env` del backend: DB_HOST/PORT/USERNAME/PASSWORD/DATABASE, JWT_SECRET, PORT, DROP_SCHEMA; SMTP y Cloudinary opcionales.

Inconsistencias detectadas:
- README de `main` pide la carpeta `backend_gym_sync`, que no existe en `main` (es otra rama).
- `docker-compose.yml` de `main` apunta a `../../backend_gym_sync`.
- El README de la rama `backend` es el de plantilla de NestJS, sin datos del proyecto.
- Existe `apps/mobile/migrate-laravel.js` (resto de un intento previo de Laravel, sin uso aparente).

## 4. Modelo de datos (41 entidades, ~185 endpoints, ~18k líneas de TS en backend)
- Identidad: users, user_profiles, roles, permissions, role_permissions, user_roles (con `gym_id`), emergency_contacts.
- Sedes: gyms (autorreferencia `parent_id`), gym_locations, gym_schedules, machine_inventory.
- Operación: gym_activities, gym_activity_schedules, gym_activity_attendance, reservations, waitlist_entries, check_ins, visits.
- Entrenamiento: exercises, routines, routine_exercises, user_training(+goals/preferences/restrictions), workout_sessions, workout_sets, physical_metrics_history, trainer_plans.
- Personal: staff_schedules, client_advisors, nutritional_appointments.
- Negocio: subscription_plans, user_subscriptions, subscription_payments.
- Otros: notifications (+templates, preferencias), conversations, direct_messages, system_settings.

## 5. Multi-tenant: cómo está resuelto hoy (punto crítico)
- La "cadena" es un registro de `gyms` con `parent_id = NULL`; la sucursal es un `gyms` con `parent_id` = cadena. No hay entidad `Tenant`.
- Roles (nivel): SUPER_ADMIN 10, GERENTE 5 (marca), RECEPCIONISTA 4, ENTRENADOR 3, NUTRICIONISTA 3, INSTRUCTOR 2, COORDINADOR 2, CLIENTE 1, PERSONAL_DE_LIMPIEZA 1.
- El aislamiento se hace a mano: `common/security/gym-scope.ts` (`getManagerGymId`, `ensureManagerMatchesResourceGym`) llamado desde cada servicio. No hay filtro global, así que olvidar la llamada en un endpoint filtra datos de otra cadena.
- `getManagerGymId` cae a `brandId` si no hay `gymId` y lo trata igual (mezcla sucursal y cadena).
- Los nombres de gimnasio son únicos globalmente (`name unique`), lo que limita a dos cadenas con una sucursal homónima.

## 6. Riesgos y deuda técnica a no copiar a ciegas
- `synchronize: true` fuera de producción y `dropSchema` por variable: no hay migraciones. En EF Core hay que crearlas desde cero.
- Seeder destructivo (TRUNCATE de todas las tablas y DROP de tablas "fantasma").
- docker-compose del backend con contraseñas y JWT por defecto (`12345`, `gymsync_jwt_secret_2026`).
- CORS `origin: true` con credenciales.
- Servicios gigantes: reservations 1518 líneas, staff 1183, users 973.
- Pocas pruebas: solo 2 `.spec` (reservations, activities) en el backend.
- Scripts sueltos en la raíz del backend (`audit-fix.js`, `print-5.js`, `check-fk.ts`, etc.).
- Endpoints sin control de rol: `/subscriptions/*` (cualquier usuario autenticado crea planes y
  suscripciones), `PUT/DELETE /system/settings`, `DELETE /metrics/:id`, `/waitlist/*`.
- La regla «reservar exige suscripción activa» solo la valida la app; la API acepta la reserva.
- Los planes de suscripción son globales: no pertenecen a ninguna cadena.
- Estados mezclados en inglés y español (`CONFIRMADA`, `CANCELLED`, `ACTIVO`, `WAITING`).

## 7. Mapa de migración hacia Stack B
| Origen | Destino |
|---|---|
Regla del curso: solo se usa lo que el PDF nombra para el Stack B. Lo demás requiere
aprobación del docente antes de agregarse.

Dentro del Stack B (sin consulta):

| Origen | Destino |
|---|---|
| NestJS + TypeORM + PostgreSQL | ASP.NET Core Web API + EF Core + SQL Server (migraciones) |
| JWT + Passport + guards por nivel | Autenticación y autorización de ASP.NET Core (cookies para el panel, JWT para la API) |
| `gym-scope.ts` manual | Tenant = Cadena + filtros globales de EF Core |
| `@nestjs/schedule` | `BackgroundService` de ASP.NET Core |
| Swagger de Nest | OpenAPI integrado de ASP.NET Core |
| Web React (panel) | MVC/Razor con plantilla de administración del PDF (AdminLTE, CoreUI, Tabler, Volt, Argon, Material) |
| App Expo/React Native | .NET MAUI con controles nativos, `Colors.xaml` y `Styles.xaml` |
| `packages/core` (hexagonal) | Carpetas de dominio y servicios dentro de `web/` |

Pendiente de consultar con el docente (no está en el PDF):

| Función del proyecto base | Tecnología usada hoy | Opción en .NET a consultar |
|---|---|---|
| Tiempo real (aforo, eventos) | Socket.io | SignalR, o refresco periódico sin librería extra |
| Archivos (fotos, videos) | Cloudinary | Almacenamiento local en el servidor o Cloudinary |
| Correo (recuperar contraseña) | Nodemailer | `SmtpClient` de .NET o MailKit |
| Notificaciones push | Expo Push | Firebase Cloud Messaging |
| Escaneo de QR en el celular | expo-camera | ZXing.Net.Maui |
| Reportes PDF | @react-pdf/renderer | QuestPDF, o imprimir la vista HTML desde el navegador |
| Mapas de sedes | Leaflet | Leaflet (JS en la vista Razor) |

## 8. Ejecución del proyecto base sin modificaciones (2026-10-08)

Se levantó cada parte siguiendo el README, sin cambiar archivos versionados (solo `.env` locales,
que el repo ignora). Base de datos: PostgreSQL 15 en Docker.

| Parte | Resultado |
|---|---|
| Backend NestJS (rama `backend`) | ✅ Compila sin errores, arranca en `:3000`, crea las 41 tablas, Swagger en `/api/docs` |
| Seed (`npm run db:seed`) | ✅ 10 cadenas, 30 sucursales, 206 usuarios, 85 ejercicios, 419 máquinas, 90 actividades, 180 horarios |
| Login por rol | ✅ Super admin, gerente, recepcionista, entrenador y cliente inician sesión |
| Permisos | ✅ Cliente y entrenador reciben 403 en usuarios, panel y check-ins |
| Aislamiento entre cadenas | ✅ En usuarios y máquinas: gerente ve solo su cadena (22 usuarios, 4 sedes), recepcionista solo su sucursal (7 usuarios), admin todo (206) |
| Panel web en desarrollo (`npm run web`) | ✅ Arranca en `:5173` y carga todas las pantallas |
| Panel web en producción (`npm run build`) | ❌ Falla con 139 errores de TypeScript (sobre todo en `packages/core` y `Dashboard`): el Dockerfile del frontend no puede construirse |
| App móvil (`npx expo start`) | ✅ Metro genera el bundle de Android (12,8 MB) sin errores |
| App móvil en emulador Android (Expo Go 54.0.8) | ✅ Abre, el cliente inicia sesión y ve su inicio con datos de la API. «No hay sucursales en 10 km» porque el emulador ubica el GPS en EE. UU. |
| App móvil: dependencias | ⚠️ 6 paquetes con versión distinta a la esperada por Expo SDK 54 (`expo-blur` 55 en lugar de 15, `expo-file-system`, `expo-secure-store`, `react-native-svg`, etc.) |
| App móvil: chequeo de tipos | ⚠️ No se ejecuta: `tsconfig.json` tiene un valor inválido en `ignoreDeprecations` |

Cómo abrir la app sin buscar a mano un Expo Go compatible: con el emulador de Android encendido,
`npx expo start --android` descarga e instala en él la versión de Expo Go que corresponde al SDK
del proyecto (54.0.8). Si la CLI pide iniciar sesión en Expo, arrancar con `EXPO_OFFLINE=1` y
abrir `exp://<IP-del-PC>:8081` en el emulador.

Credenciales del seed: `admin@gymsync.com` y todos los usuarios generados con la contraseña
`Aaron123*`. Un usuario de ejemplo por rol: gerente `ana.iglesias@gmail.com`, recepcionista
`adrian.alvarez1@gmail.com`, cliente `adrian.alvarez3@gmail.com`, instructor
`alberto.cortes1@gmail.com`, entrenador `alejandro.martin1@gmail.com`.

Observaciones que afectan la migración:

- El seed **no crea planes de suscripción ni rutinas**, y las reservas exigen suscripción activa:
  con los datos de prueba, un cliente no puede reservar hasta que alguien cree un plan. El seed
  de la versión migrada debe incluirlos.
- Roles NUTRICIONISTA, COORDINADOR y PERSONAL_DE_LIMPIEZA existen pero el seed no les asigna
  usuarios.
- El data source del seeder registra 33 entidades y la aplicación 41: las tablas de personal,
  planes del entrenador, citas nutricionales y visitas solo existen si antes se arrancó la API.
- Lo que funciona hoy (login por rol, permisos, aislamiento por cadena y sucursal) es el
  comportamiento de referencia que la versión migrada debe reproducir y probar.

## 9. Decisiones abiertas
1. Estrategia multi-tenant: propuesta en `contexto-proyecto.md`.
2. Qué hacer con funciones del repo base fuera del alcance mínimo (mensajería, lista de espera, citas nutricionales, geolocalización, modo offline).
3. Qué rama de backend es la vigente (`backend` vs `backend2`; se usó `backend`).
