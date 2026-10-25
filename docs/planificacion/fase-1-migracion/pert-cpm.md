# Resultado PERT / CPM

> Archivo generado por `PertCpm.cs` a partir de `actividades.csv`. No editar a mano: cambiar el CSV y volver a ejecutar `dotnet run PertCpm.cs`.

Unidad: días de trabajo del responsable (se trabaja de lunes a domingo). TE = (O + 4M + P) ÷ 6 · σ = (P − O) ÷ 6.

## 1. Estimación PERT

| Act. | EDT | Paquete de trabajo | Responsable | Pred. | O | M | P | TE | σ |
|---|---|---|---|---|---|---|---|---|---|
| A | 2.1 | Diagnóstico del proyecto base | Pareja | — | 1 | 2 | 3 | 2,00 | 0,33 |
| B | 1.1 | Plan del proyecto (EDT, PERT, CPM) | Pareja | A | 1 | 2 | 3 | 2,00 | 0,33 |
| C | 1.2 | Repositorio y tablero Planner | Pareja | B | 1 | 1 | 2 | 1,17 | 0,17 |
| D | 2.2 | Requisitos: casos de uso e historias | Pareja | A, B, C* | 2 | 3 | 5 | 3,17 | 0,50 |
| E | 2.3 | Modelo de datos multi-tenant | Pareja | D | 2 | 3 | 5 | 3,17 | 0,50 |
| F | 2.4 | Mockups de la web | Jean | D, E* | 2 | 3 | 4 | 3,00 | 0,33 |
| G | 2.5 | Mockups de la app | Melani | D, E* | 2 | 3 | 4 | 3,00 | 0,33 |
| H | 2.6 | Contrato de la API | Melani | E, G* | 1 | 2 | 3 | 2,00 | 0,33 |
| I | 6.1 | Infraestructura de producción | Melani | C, H* | 1 | 2 | 4 | 2,17 | 0,50 |
| W01 | 3.1 | Base del panel, autenticación y roles | Jean | C, E, F | 3 | 4 | 7 | 4,33 | 0,67 |
| W02 | 3.2 | Cadenas y sucursales | Jean | W01 | 3 | 4 | 7 | 4,33 | 0,67 |
| W03 | 3.3 | Usuarios y membresías | Jean | W02 | 3 | 4 | 7 | 4,33 | 0,67 |
| W04 | 3.4 | Personal y asignaciones | Jean | W03 | 2 | 3 | 5 | 3,17 | 0,50 |
| W05 | 3.5 | Ejercicios y rutinas | Jean | W03, W04 | 2 | 4 | 6 | 4,00 | 0,67 |
| W06 | 3.6 | Entrenamiento y métricas | Jean | W05 | 2 | 4 | 6 | 4,00 | 0,67 |
| W07 | 3.7 | Actividades y horarios | Jean | W02, W04, W06* | 2 | 4 | 6 | 4,00 | 0,67 |
| W08 | 3.8 | Inventario de máquinas | Jean | W02, W07* | 1 | 2 | 3 | 2,00 | 0,33 |
| W09 | 3.9 | Planes de suscripción y pagos | Jean | W02, W03, W08* | 1 | 2 | 4 | 2,17 | 0,50 |
| W10 | 3.10 | Reservas | Jean | W07, W09 | 3 | 4 | 8 | 4,50 | 0,83 |
| W11 | 3.11 | Lista de espera | Jean | W10 | 1 | 1 | 2 | 1,17 | 0,17 |
| W12 | 3.12 | Control de acceso y auditoría | Jean | W03, W10, W11* | 2 | 3 | 5 | 3,17 | 0,50 |
| W13 | 3.13 | Plan nutricional y citas | Jean | W04, W12* | 1 | 2 | 4 | 2,17 | 0,50 |
| W14 | 3.14 | Notificaciones (web y API) | Jean | W10, W13* | 1 | 2 | 4 | 2,17 | 0,50 |
| W15 | 3.15 | Panel de resumen | Jean | W10, W12, W14* | 2 | 3 | 5 | 3,17 | 0,50 |
| W16 | 3.16 | Reportes de operación | Jean | W07, W08, W12, W15* | 2 | 3 | 5 | 3,17 | 0,50 |
| W17 | 3.17 | Reportes de gestión | Jean | W16 | 2 | 3 | 5 | 3,17 | 0,50 |
| M01 | 4.1 | Base de la app | Melani | G, H, I* | 2 | 3 | 5 | 3,17 | 0,50 |
| M02 | 4.2 | Autenticación y perfil | Melani | M01, W03 | 2 | 4 | 6 | 4,00 | 0,67 |
| M03 | 4.3 | Rutinas y modo entrenamiento | Melani | M02, W05 | 2 | 3 | 5 | 3,17 | 0,50 |
| M04 | 4.4 | Historial de entrenamiento | Melani | M03, W06 | 2 | 3 | 5 | 3,17 | 0,50 |
| M05 | 4.5 | Seguimiento y objetivos | Melani | W06, M04* | 2 | 3 | 5 | 3,17 | 0,50 |
| M06 | 4.6 | Vistas del entrenador | Melani | W04, W05, M05* | 3 | 4 | 7 | 4,33 | 0,67 |
| M07 | 4.7 | Inicio y sedes | Melani | W07, W08, M06* | 3 | 4 | 7 | 4,33 | 0,67 |
| M08 | 4.8 | Reservas y carnet QR | Melani | M07, W11, W12 | 3 | 4 | 7 | 4,33 | 0,67 |
| M09 | 4.9 | Mi plan y plan nutricional | Melani | W09, W13, M08* | 1 | 2 | 4 | 2,17 | 0,50 |
| M10 | 4.10 | Vistas del instructor y nutricionista | Melani | W07, W13, M09* | 2 | 3 | 6 | 3,33 | 0,67 |
| M11 | 4.11 | Vistas de recepción y gerencia | Melani | W12, W15, M10* | 2 | 4 | 6 | 4,00 | 0,67 |
| M12 | 4.12 | Notificaciones (app) | Melani | W14, M11* | 1 | 2 | 4 | 2,17 | 0,50 |
| K | 5.2 | Manual de usuario | Melani | M12 | 2 | 3 | 5 | 3,17 | 0,50 |
| J | 5.1 | Verificación de la migración (pruebas integrales) | Pareja | W17, M12, K* | 2 | 3 | 5 | 3,17 | 0,50 |
| L | 6.2 | Release de la migración | Pareja | I, J, K | 1 | 1 | 2 | 1,17 | 0,17 |

\* Dependencia por recurso: la misma persona termina su paquete anterior antes de empezar este.

## 2. CPM: ida, vuelta y holgura

| Act. | Paquete | IT | Dur. | FT | IL | FL | Holgura | Ruta crítica |
|---|---|---|---|---|---|---|---|---|
| A | Diagnóstico del proyecto base | 0,00 | 2,00 | 2,00 | 0,00 | 2,00 | 0,00 | **Sí** |
| B | Plan del proyecto (EDT, PERT, CPM) | 2,00 | 2,00 | 4,00 | 2,00 | 4,00 | 0,00 | **Sí** |
| C | Repositorio y tablero Planner | 4,00 | 1,17 | 5,17 | 4,00 | 5,17 | 0,00 | **Sí** |
| D | Requisitos: casos de uso e historias | 5,17 | 3,17 | 8,34 | 5,17 | 8,34 | 0,00 | **Sí** |
| E | Modelo de datos multi-tenant | 8,34 | 3,17 | 11,51 | 8,34 | 11,51 | 0,00 | **Sí** |
| F | Mockups de la web | 11,51 | 3,00 | 14,51 | 11,51 | 14,51 | 0,00 | **Sí** |
| G | Mockups de la app | 11,51 | 3,00 | 14,51 | 23,17 | 26,17 | 11,66 |  |
| H | Contrato de la API | 14,51 | 2,00 | 16,51 | 26,17 | 28,17 | 11,66 |  |
| W01 | Base del panel, autenticación y roles | 14,51 | 4,33 | 18,84 | 14,51 | 18,84 | 0,00 | **Sí** |
| I | Infraestructura de producción | 16,51 | 2,17 | 18,68 | 28,17 | 30,34 | 11,66 |  |
| M01 | Base de la app | 18,68 | 3,17 | 21,85 | 30,34 | 33,51 | 11,66 |  |
| W02 | Cadenas y sucursales | 18,84 | 4,33 | 23,17 | 18,84 | 23,17 | 0,00 | **Sí** |
| W03 | Usuarios y membresías | 23,17 | 4,33 | 27,50 | 23,17 | 27,50 | 0,00 | **Sí** |
| W04 | Personal y asignaciones | 27,50 | 3,17 | 30,67 | 27,50 | 30,67 | 0,00 | **Sí** |
| M02 | Autenticación y perfil | 27,50 | 4,00 | 31,50 | 33,51 | 37,51 | 6,01 |  |
| W05 | Ejercicios y rutinas | 30,67 | 4,00 | 34,67 | 30,67 | 34,67 | 0,00 | **Sí** |
| W06 | Entrenamiento y métricas | 34,67 | 4,00 | 38,67 | 34,67 | 38,67 | 0,00 | **Sí** |
| M03 | Rutinas y modo entrenamiento | 34,67 | 3,17 | 37,84 | 37,51 | 40,68 | 2,84 |  |
| W07 | Actividades y horarios | 38,67 | 4,00 | 42,67 | 38,67 | 42,67 | 0,00 | **Sí** |
| M04 | Historial de entrenamiento | 38,67 | 3,17 | 41,84 | 40,68 | 43,85 | 2,01 |  |
| M05 | Seguimiento y objetivos | 41,84 | 3,17 | 45,01 | 43,85 | 47,02 | 2,01 |  |
| W08 | Inventario de máquinas | 42,67 | 2,00 | 44,67 | 42,67 | 44,67 | 0,00 | **Sí** |
| W09 | Planes de suscripción y pagos | 44,67 | 2,17 | 46,84 | 44,67 | 46,84 | 0,00 | **Sí** |
| M06 | Vistas del entrenador | 45,01 | 4,33 | 49,34 | 47,02 | 51,35 | 2,01 |  |
| W10 | Reservas | 46,84 | 4,50 | 51,34 | 46,84 | 51,34 | 0,00 | **Sí** |
| M07 | Inicio y sedes | 49,34 | 4,33 | 53,67 | 51,35 | 55,68 | 2,01 |  |
| W11 | Lista de espera | 51,34 | 1,17 | 52,51 | 51,34 | 52,51 | 0,00 | **Sí** |
| W12 | Control de acceso y auditoría | 52,51 | 3,17 | 55,68 | 52,51 | 55,68 | 0,00 | **Sí** |
| W13 | Plan nutricional y citas | 55,68 | 2,17 | 57,85 | 57,84 | 60,01 | 2,16 |  |
| M08 | Reservas y carnet QR | 55,68 | 4,33 | 60,01 | 55,68 | 60,01 | 0,00 | **Sí** |
| W14 | Notificaciones (web y API) | 57,85 | 2,17 | 60,02 | 60,17 | 62,34 | 2,32 |  |
| M09 | Mi plan y plan nutricional | 60,01 | 2,17 | 62,18 | 60,01 | 62,18 | 0,00 | **Sí** |
| W15 | Panel de resumen | 60,02 | 3,17 | 63,19 | 62,34 | 65,51 | 2,32 |  |
| M10 | Vistas del instructor y nutricionista | 62,18 | 3,33 | 65,51 | 62,18 | 65,51 | 0,00 | **Sí** |
| W16 | Reportes de operación | 63,19 | 3,17 | 66,36 | 68,51 | 71,68 | 5,32 |  |
| M11 | Vistas de recepción y gerencia | 65,51 | 4,00 | 69,51 | 65,51 | 69,51 | 0,00 | **Sí** |
| W17 | Reportes de gestión | 66,36 | 3,17 | 69,53 | 71,68 | 74,85 | 5,32 |  |
| M12 | Notificaciones (app) | 69,51 | 2,17 | 71,68 | 69,51 | 71,68 | 0,00 | **Sí** |
| K | Manual de usuario | 71,68 | 3,17 | 74,85 | 71,68 | 74,85 | 0,00 | **Sí** |
| J | Verificación de la migración (pruebas integrales) | 74,85 | 3,17 | 78,02 | 74,85 | 78,02 | 0,00 | **Sí** |
| L | Release de la migración | 78,02 | 1,17 | 79,19 | 78,02 | 79,19 | 0,00 | **Sí** |

IT/FT: inicio y fin más temprano (ida) · IL/FL: inicio y fin más tardío (vuelta) · Holgura = IL − IT.

## 3. Ruta crítica y duración del proyecto

**Ruta crítica:** A › B › C › D › E › F › W01 › W02 › W03 › W04 › W05 › W06 › W07 › W08 › W09 › W10 › W11 › W12 › M08 › M09 › M10 › M11 › M12 › K › J › L

- Duración esperada: **79,19 días** (≈ 11,3 iteraciones de 7 días).
- Desviación de la ruta crítica: σ = √(Σσ²) = √7,50 = **2,74 días**.
- Con 84 % de probabilidad termina en 81,9 días o menos (TE + 1σ).
- Con 95 % de probabilidad termina en 83,7 días o menos (TE + 1,645σ).

## 4. Carga por responsable

| Responsable | Paquetes | Días (Σ TE) |
|---|---|---|
| Jean | 18 | 58,02 |
| Melani | 16 | 51,68 |
| Pareja | 7 | 15,85 |

## 5. Fechas para Planner e iteraciones

Día 1 = lunes en que empieza la iteración 1 (iteraciones de lunes a domingo). Inicio = día siguiente a IT; vencimiento = FT redondeado hacia arriba.

| Tarea en Planner | Responsable | Inicio | Vencimiento | Iteración | Etiquetas |
|---|---|---|---|---|---|
| [2.1] #— Diagnóstico del proyecto base | Pareja | Día 1 | Día 2 | 1 | Fase 2 · Ruta crítica |
| [1.1] #— Plan del proyecto (EDT, PERT, CPM) | Pareja | Día 3 | Día 4 | 1 | Fase 1 · Ruta crítica |
| [1.2] #— Repositorio y tablero Planner | Pareja | Día 5 | Día 6 | 1 | Fase 1 · Ruta crítica |
| [2.2] #— Requisitos: casos de uso e historias | Pareja | Día 6 | Día 9 | 1–2 | Fase 2 · Ruta crítica |
| [2.3] #— Modelo de datos multi-tenant | Pareja | Día 9 | Día 12 | 2 | Fase 2 · Ruta crítica |
| [2.4] #— Mockups de la web | Jean | Día 12 | Día 15 | 2–3 | Fase 2 · Ruta crítica |
| [2.5] #— Mockups de la app | Melani | Día 12 | Día 15 | 2–3 | Fase 2 |
| [2.6] #— Contrato de la API | Melani | Día 15 | Día 17 | 3 | Fase 2 |
| [3.1] #— Base del panel, autenticación y roles | Jean | Día 15 | Día 19 | 3 | Fase 3 · Web · Ruta crítica |
| [6.1] #— Infraestructura de producción | Melani | Día 17 | Día 19 | 3 | Fase 6 |
| [4.1] #— Base de la app | Melani | Día 19 | Día 22 | 3–4 | Fase 4 · Móvil |
| [3.2] #— Cadenas y sucursales | Jean | Día 19 | Día 24 | 3–4 | Fase 3 · Web · Ruta crítica |
| [3.3] #— Usuarios y membresías | Jean | Día 24 | Día 28 | 4 | Fase 3 · Web · Ruta crítica |
| [3.4] #— Personal y asignaciones | Jean | Día 28 | Día 31 | 4–5 | Fase 3 · Web · Ruta crítica |
| [4.2] #— Autenticación y perfil | Melani | Día 28 | Día 32 | 4–5 | Fase 4 · Móvil |
| [3.5] #— Ejercicios y rutinas | Jean | Día 31 | Día 35 | 5 | Fase 3 · Web · Ruta crítica |
| [3.6] #— Entrenamiento y métricas | Jean | Día 35 | Día 39 | 5–6 | Fase 3 · Web · Ruta crítica |
| [4.3] #— Rutinas y modo entrenamiento | Melani | Día 35 | Día 38 | 5–6 | Fase 4 · Móvil |
| [3.7] #— Actividades y horarios | Jean | Día 39 | Día 43 | 6–7 | Fase 3 · Web · Ruta crítica |
| [4.4] #— Historial de entrenamiento | Melani | Día 39 | Día 42 | 6 | Fase 4 · Móvil |
| [4.5] #— Seguimiento y objetivos | Melani | Día 42 | Día 46 | 6–7 | Fase 4 · Móvil |
| [3.8] #— Inventario de máquinas | Jean | Día 43 | Día 45 | 7 | Fase 3 · Web · Ruta crítica |
| [3.9] #— Planes de suscripción y pagos | Jean | Día 45 | Día 47 | 7 | Fase 3 · Web · Ruta crítica |
| [4.6] #— Vistas del entrenador | Melani | Día 46 | Día 50 | 7–8 | Fase 4 · Móvil |
| [3.10] #— Reservas | Jean | Día 47 | Día 52 | 7–8 | Fase 3 · Web · Ruta crítica |
| [4.7] #— Inicio y sedes | Melani | Día 50 | Día 54 | 8 | Fase 4 · Móvil |
| [3.11] #— Lista de espera | Jean | Día 52 | Día 53 | 8 | Fase 3 · Web · Ruta crítica |
| [3.12] #— Control de acceso y auditoría | Jean | Día 53 | Día 56 | 8 | Fase 3 · Web · Ruta crítica |
| [3.13] #— Plan nutricional y citas | Jean | Día 56 | Día 58 | 8–9 | Fase 3 · Web |
| [4.8] #— Reservas y carnet QR | Melani | Día 56 | Día 61 | 8–9 | Fase 4 · Móvil · Ruta crítica |
| [3.14] #— Notificaciones (web y API) | Jean | Día 58 | Día 61 | 9 | Fase 3 · Web |
| [4.9] #— Mi plan y plan nutricional | Melani | Día 61 | Día 63 | 9 | Fase 4 · Móvil · Ruta crítica |
| [3.15] #— Panel de resumen | Jean | Día 61 | Día 64 | 9–10 | Fase 3 · Web |
| [4.10] #— Vistas del instructor y nutricionista | Melani | Día 63 | Día 66 | 9–10 | Fase 4 · Móvil · Ruta crítica |
| [3.16] #— Reportes de operación | Jean | Día 64 | Día 67 | 10 | Fase 3 · Web |
| [4.11] #— Vistas de recepción y gerencia | Melani | Día 66 | Día 70 | 10 | Fase 4 · Móvil · Ruta crítica |
| [3.17] #— Reportes de gestión | Jean | Día 67 | Día 70 | 10 | Fase 3 · Web |
| [4.12] #— Notificaciones (app) | Melani | Día 70 | Día 72 | 10–11 | Fase 4 · Móvil · Ruta crítica |
| [5.2] #— Manual de usuario | Melani | Día 72 | Día 75 | 11 | Fase 5 · Ruta crítica |
| [5.1] #— Verificación de la migración (pruebas integrales) | Pareja | Día 75 | Día 79 | 11–12 | Fase 5 · Ruta crítica |
| [6.2] #— Release de la migración | Pareja | Día 79 | Día 80 | 12 | Fase 6 · Ruta crítica |

El `#—` se reemplaza por el número del issue cuando se cree en GitHub (formato del PDF: `[EDT] #issue Título`).

## 6. Validaciones

- Paquetes con TE > 5 días (regla «paquete ≤ 1 semana»): ninguno ✔
- Dependencias sin ciclos y todas las predecesoras existen ✔
- En todas las actividades O ≤ M ≤ P ✔
