# Resultado PERT / CPM

> Archivo generado por `PertCpm.cs` a partir de `actividades.csv`. No editar a mano: cambiar el CSV y volver a ejecutar `dotnet run PertCpm.cs`.

Unidad: días de trabajo de la pareja (se trabaja de jueves a miércoles). TE = (O + 4M + P) ÷ 6 · σ = (P − O) ÷ 6.

## 1. Estimación PERT

| Act. | EDT | Paquete de trabajo | Responsable | Pred. | O | M | P | TE | σ |
|---|---|---|---|---|---|---|---|---|---|
| A | 1.1 | Alcance, contratos y criterios de aceptación | Pareja | — | 1 | 1 | 1 | 1,00 | 0,00 |
| B | 2.1 | Propuesta visual oscura y validación de plantilla | Jean | A | 1 | 2 | 3 | 2,00 | 0,33 |
| C | 2.2 | Diseño de vistas móviles y estilo común | Melanie | A | 2 | 3 | 4 | 3,00 | 0,33 |
| D | 2.3 | Implementación visual del panel y responsividad | Jean | B | 2 | 3 | 4 | 3,00 | 0,33 |
| E | 3.1 | RF-02: permisos, consentimiento y API de perfil portable | Jean | D | 2 | 3 | 4 | 3,00 | 0,33 |
| F | 3.2 | RF-02: controles de consentimiento en móvil | Melanie | C, E | 1 | 2 | 3 | 2,00 | 0,33 |
| G | 4.1 | RF-03: beneficios, vales y reglas de canje | Jean | E | 2 | 3 | 4 | 3,00 | 0,33 |
| H | 4.2 | RF-03: visualización y canje desde la app | Melanie | F, G | 1 | 2 | 3 | 2,00 | 0,33 |
| I | 5.1 | RF-04: criterios y detección de inactividad | Jean | G | 1 | 2 | 3 | 2,00 | 0,33 |
| J | 5.2 | RF-04: alertas y gestión desde el panel | Jean | I | 1 | 2 | 3 | 2,00 | 0,33 |
| K | 6.1 | RF-05: confirmación y conciliación de pagos QR | Jean | J | 1 | 3 | 5 | 3,00 | 0,67 |
| L | 6.2 | RF-05: flujo de pago y comprobantes móvil | Melanie | H, K | 1 | 2 | 3 | 2,00 | 0,33 |
| M | 7.1 | RF-01: flujo de video y revisión por entrenador | Jean | K | 1 | 2 | 3 | 2,00 | 0,33 |
| N | 7.2 | RF-01: integración de análisis y validación humana | Jean | M | 1 | 3 | 5 | 3,00 | 0,67 |
| O | 7.3 | RF-01: captura y seguimiento del resultado en app | Melanie | L, N | 1 | 3 | 5 | 3,00 | 0,67 |
| P | 8.1 | RF-06: contenido de rutinas, simulación y nutrición | Melanie | O | 1 | 2 | 3 | 2,00 | 0,33 |
| Q | 8.2 | RF-06: prueba funcional de simulación en móvil | Melanie | P | 1 | 3 | 5 | 3,00 | 0,67 |
| R | 8.3 | RF-06: integración del contenido nutricional en web | Jean | N | 1 | 2 | 3 | 2,00 | 0,33 |
| S | 9.1 | Pruebas integrales, seguridad y regresión | Pareja | Q, R | 2 | 3 | 4 | 3,00 | 0,33 |
| T | 9.2 | Correcciones, manual, publicación y defensa | Pareja | S | 1 | 2 | 3 | 2,00 | 0,33 |

\* Dependencia por recurso: la misma persona termina su paquete anterior antes de empezar este.

## 2. CPM: ida, vuelta y holgura

| Act. | Paquete | IT | Dur. | FT | IL | FL | Holgura | Ruta crítica |
|---|---|---|---|---|---|---|---|---|
| A | Alcance, contratos y criterios de aceptación | 0,00 | 1,00 | 1,00 | 0,00 | 1,00 | 0,00 | **Sí** |
| B | Propuesta visual oscura y validación de plantilla | 1,00 | 2,00 | 3,00 | 1,00 | 3,00 | 0,00 | **Sí** |
| C | Diseño de vistas móviles y estilo común | 1,00 | 3,00 | 4,00 | 15,00 | 18,00 | 14,00 |  |
| D | Implementación visual del panel y responsividad | 3,00 | 3,00 | 6,00 | 3,00 | 6,00 | 0,00 | **Sí** |
| E | RF-02: permisos, consentimiento y API de perfil portable | 6,00 | 3,00 | 9,00 | 6,00 | 9,00 | 0,00 | **Sí** |
| F | RF-02: controles de consentimiento en móvil | 9,00 | 2,00 | 11,00 | 18,00 | 20,00 | 9,00 |  |
| G | RF-03: beneficios, vales y reglas de canje | 9,00 | 3,00 | 12,00 | 9,00 | 12,00 | 0,00 | **Sí** |
| H | RF-03: visualización y canje desde la app | 12,00 | 2,00 | 14,00 | 20,00 | 22,00 | 8,00 |  |
| I | RF-04: criterios y detección de inactividad | 12,00 | 2,00 | 14,00 | 12,00 | 14,00 | 0,00 | **Sí** |
| J | RF-04: alertas y gestión desde el panel | 14,00 | 2,00 | 16,00 | 14,00 | 16,00 | 0,00 | **Sí** |
| K | RF-05: confirmación y conciliación de pagos QR | 16,00 | 3,00 | 19,00 | 16,00 | 19,00 | 0,00 | **Sí** |
| L | RF-05: flujo de pago y comprobantes móvil | 19,00 | 2,00 | 21,00 | 22,00 | 24,00 | 3,00 |  |
| M | RF-01: flujo de video y revisión por entrenador | 19,00 | 2,00 | 21,00 | 19,00 | 21,00 | 0,00 | **Sí** |
| N | RF-01: integración de análisis y validación humana | 21,00 | 3,00 | 24,00 | 21,00 | 24,00 | 0,00 | **Sí** |
| O | RF-01: captura y seguimiento del resultado en app | 24,00 | 3,00 | 27,00 | 24,00 | 27,00 | 0,00 | **Sí** |
| R | RF-06: integración del contenido nutricional en web | 24,00 | 2,00 | 26,00 | 30,00 | 32,00 | 6,00 |  |
| P | RF-06: contenido de rutinas, simulación y nutrición | 27,00 | 2,00 | 29,00 | 27,00 | 29,00 | 0,00 | **Sí** |
| Q | RF-06: prueba funcional de simulación en móvil | 29,00 | 3,00 | 32,00 | 29,00 | 32,00 | 0,00 | **Sí** |
| S | Pruebas integrales, seguridad y regresión | 32,00 | 3,00 | 35,00 | 32,00 | 35,00 | 0,00 | **Sí** |
| T | Correcciones, manual, publicación y defensa | 35,00 | 2,00 | 37,00 | 35,00 | 37,00 | 0,00 | **Sí** |

IT/FT: inicio y fin más temprano (ida) · IL/FL: inicio y fin más tardío (vuelta) · Holgura = IL − IT.

## 3. Ruta crítica y duración del proyecto

**Ruta crítica:** A › B › D › E › G › I › J › K › M › N › O › P › Q › S › T

- Duración esperada: **37,00 días** (≈ 5,3 iteraciones de 7 días).
- Desviación de la ruta crítica: σ = √(Σσ²) = √2,88 = **1,70 días**.
- Con 84 % de probabilidad termina en 38,7 días o menos (TE + 1σ).
- Con 95 % de probabilidad termina en 39,8 días o menos (TE + 1,645σ).

## 4. Carga por responsable

| Responsable | Paquetes | Días (Σ TE) |
|---|---|---|
| Jean | 10 | 25,00 |
| Melanie | 7 | 17,00 |
| Pareja | 3 | 6,00 |

## 5. Fechas, iteraciones e issues de GitHub

Día 1 = jueves 15/10/2026, inicio de la iteración 1 (iteraciones de jueves a miércoles). Inicio = día siguiente a IT; vencimiento = FT redondeado hacia arriba.

| Issue (formato del PDF) | Responsable | Inicio | Vencimiento | Iteración | Etiquetas |
|---|---|---|---|---|---|
| [1.1] #50 Alcance, contratos y criterios de aceptación | Pareja | jue 15/10 | jue 15/10 | 1 | Paquete 1.0 · Web y Móvil · Ruta crítica |
| [2.1] #51 Propuesta visual oscura y validación de plantilla | Jean | vie 16/10 | sáb 17/10 | 1 | Paquete 2.0 · Web · Ruta crítica |
| [2.2] #52 Diseño de vistas móviles y estilo común | Melanie | vie 16/10 | dom 18/10 | 1 | Paquete 2.0 · Móvil |
| [2.3] #53 Implementación visual del panel y responsividad | Jean | dom 18/10 | mar 20/10 | 1 | Paquete 2.0 · Web · Ruta crítica |
| [3.1] #54 RF-02: permisos, consentimiento y API de perfil portable | Jean | mié 21/10 | vie 23/10 | 1–2 | Paquete 3.0 · Web · Ruta crítica |
| [3.2] #55 RF-02: controles de consentimiento en móvil | Melanie | sáb 24/10 | dom 25/10 | 2 | Paquete 3.0 · Móvil |
| [4.1] #56 RF-03: beneficios, vales y reglas de canje | Jean | sáb 24/10 | lun 26/10 | 2 | Paquete 4.0 · Web · Ruta crítica |
| [4.2] #57 RF-03: visualización y canje desde la app | Melanie | mar 27/10 | mié 28/10 | 2 | Paquete 4.0 · Móvil |
| [5.1] #58 RF-04: criterios y detección de inactividad | Jean | mar 27/10 | mié 28/10 | 2 | Paquete 5.0 · Web · Ruta crítica |
| [5.2] #59 RF-04: alertas y gestión desde el panel | Jean | jue 29/10 | vie 30/10 | 3 | Paquete 5.0 · Web · Ruta crítica |
| [6.1] #60 RF-05: confirmación y conciliación de pagos QR | Jean | sáb 31/10 | lun 02/11 | 3 | Paquete 6.0 · Web · Ruta crítica |
| [6.2] #61 RF-05: flujo de pago y comprobantes móvil | Melanie | mar 03/11 | mié 04/11 | 3 | Paquete 6.0 · Móvil |
| [7.1] #62 RF-01: flujo de video y revisión por entrenador | Jean | mar 03/11 | mié 04/11 | 3 | Paquete 7.0 · Web · Ruta crítica |
| [7.2] #63 RF-01: integración de análisis y validación humana | Jean | jue 05/11 | sáb 07/11 | 4 | Paquete 7.0 · Web · Ruta crítica |
| [7.3] #64 RF-01: captura y seguimiento del resultado en app | Melanie | dom 08/11 | mar 10/11 | 4 | Paquete 7.0 · Móvil · Ruta crítica |
| [8.3] #67 RF-06: integración del contenido nutricional en web | Jean | dom 08/11 | lun 09/11 | 4 | Paquete 8.0 · Web |
| [8.1] #65 RF-06: contenido de rutinas, simulación y nutrición | Melanie | mié 11/11 | jue 12/11 | 4–5 | Paquete 8.0 · Móvil · Ruta crítica |
| [8.2] #66 RF-06: prueba funcional de simulación en móvil | Melanie | vie 13/11 | dom 15/11 | 5 | Paquete 8.0 · Móvil · Ruta crítica |
| [9.1] #68 Pruebas integrales, seguridad y regresión | Pareja | lun 16/11 | mié 18/11 | 5 | Paquete 9.0 · Web y Móvil · Ruta crítica |
| [9.2] #69 Correcciones, manual, publicación y defensa | Pareja | jue 19/11 | vie 20/11 | 6 | Paquete 9.0 · Web y Móvil · Ruta crítica |

Formato del PDF del curso: `[EDT] #issue Título`. La rama de cada actividad es `feature/<issue>-<descripcion>`.

## 6. Validaciones

- Paquetes con TE > 5 días (regla «paquete ≤ 1 semana»): ninguno ✔
- Dependencias sin ciclos y todas las predecesoras existen ✔
- En todas las actividades O ≤ M ≤ P ✔
