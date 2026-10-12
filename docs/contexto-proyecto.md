# Contexto del proyecto

## 1. Lo que pidió el docente (transcripción de la clase)

- Gestión de gimnasios: dieta, alimentación, rutina, entrenador y seguimiento completo.
- Multi-tenant; la meta es manejar todos los gimnasios de Santa Cruz.
- Ejemplo: una cadena (AES) tiene varias sucursales y dentro de ella se ven todas.
- Si el socio se va de un gimnasio a otro, mantiene toda su información (al menos entre sucursales).
- Simulaciones de ejercicios (animaciones).
- Seguimiento del socio.
- El socio sube videos de cómo hace los ejercicios; la IA evalúa si está bien hecho. El resultado
  **no va al socio**: va al instructor o entrenador, que verifica lo que dijo la IA. La valoración
  final es del entrenador, y luego el entrenador retroalimenta a la IA.
- Proyecto ya hecho: migrarlo a Laravel o .NET Core.
- Recompensas con vales, cupones, etc.
- **Antes de aumentar cosas, primero migrarlo y que funcione.**

Stack elegido: **Stack B** (ASP.NET Core + .NET MAUI + SQL Server + EF Core), recomendado por
el docente por el carácter multi-tenant del proyecto. Regla: solo tecnologías del PDF del curso;
cualquier otra se consulta antes con el docente.

## 2. Pedido del docente frente a lo que ya existe

| Pedido | ¿Está en el proyecto base? | Requerimiento del análisis (Word) |
|---|---|---|
| Multi-tenant por cadena con sucursales | Sí, pero frágil (cadena = gimnasio sin padre, filtro manual) | Base de la migración |
| Rutinas y entrenador | Sí (rutinas, ejercicios, sesiones, series, planes del entrenador) | — |
| Seguimiento | Sí (métricas físicas, historial de sesiones, reportes) | RF-04 alertas de abandono |
| Dieta y alimentación | **Parcial**: plan del entrenador con kcal, macros y comidas en `jsonb`; citas con nutricionista | RF-06 nutrición integrada |
| Mantener la información al cambiar de gimnasio | No | RF-02 perfil deportivo portable |
| Animaciones de ejercicios | Parcial: videos por URL en la biblioteca | RF-06 simulaciones 3D |
| Video + IA + validación del entrenador | No | RF-01 |
| Recompensas, vales y cupones | No | RF-03 |
| Pago con QR boliviano | No (pagos de suscripción sin pasarela) | RF-05 |

## 3. Decisiones de diseño que se toman ya, en la migración

Las mejoras se implementan después, pero dos de ellas condicionan el modelo de datos. Si la
migración no las contempla, habría que rehacer tablas más adelante.

### 3.1 Tenant = Cadena, y la sucursal pertenece a la cadena

- `Cadena` es el inquilino. `Sucursal` pertenece a una cadena.
- Los datos operativos de una cadena llevan `CadenaId` y EF Core los filtra con un filtro global
  de consulta: sucursales, máquinas, actividades, horarios, reservas, check-ins, planes de
  suscripción, pagos y personal.
- El filtro es automático; no depende de que cada servicio recuerde aplicarlo (que es el
  problema del proyecto base, ver diagnóstico §5).
- Una sola base de datos: encaja con el hosting gratuito del PDF (una BD por cuenta).

### 3.2 El socio es una persona global; su relación con cada cadena es una membresía

El docente pidió que el socio conserve su información al cambiar de gimnasio. Por eso:

- `Persona` (cuenta de usuario y datos personales) **no** pertenece a una cadena.
- El historial deportivo (métricas, sesiones de entrenamiento, objetivos, restricciones,
  evaluaciones) se vincula a la persona, no a la cadena.
- `Membresia` une persona + cadena (+ sucursal habitual) y es lo que el filtro de tenant usa para
  saber qué socios ve cada cadena.
- Entre sucursales de la misma cadena el historial se ve sin hacer nada; entre cadenas, más
  adelante (RF-02), se agrega el consentimiento explícito del socio.

Si en la migración el historial quedara con `CadenaId`, RF-02 obligaría a mover datos entre
inquilinos. Con este diseño solo hay que agregar el consentimiento.

### 3.3 Datos JSON del proyecto base

PostgreSQL `jsonb` (por ejemplo `meal_plan` del plan del entrenador) no existe en SQL Server. Se
migra a tablas normalizadas (plan → comidas → alimentos) o, si solo se guarda y se muestra, a
`nvarchar(max)` con JSON. Se decide en la historia de usuario correspondiente.

## 4. Recomendaciones adicionales

1. **Confirmar con el docente si la dieta es parte del alcance base.** El docente la nombra en
   primer lugar ("manejo de dieta, alimentación, rutina"), mientras que el Word la deja en RF-06
   como mejora posterior. El proyecto base ya trae una parte. Migrar esa parte tal cual y dejar
   la nutrición completa para RF-06 es lo más coherente con "primero migrar".
2. **Aclarar quién revisa la IA: instructor o entrenador.** La transcripción usa los dos términos y
   el proyecto base los distingue (INSTRUCTOR dicta clases grupales; ENTRENADOR acompaña a socios
   en sala). Para RF-01 lo lógico es el entrenador asignado al socio, pero conviene confirmarlo.
3. **No migrar la deuda técnica:** sin `synchronize` (todo por migraciones de EF Core), seeders
   no destructivos, sin claves por defecto en archivos, CORS restringido a los orígenes propios.

## 5. Preguntas para el docente

1. ¿La dieta completa entra en la migración o queda como mejora (RF-06)?
2. ¿La revisión de la IA la hace el entrenador asignado al socio, el instructor o ambos?
3. ¿Se migra todo el proyecto base o un núcleo? Funciones candidatas a quedar fuera de la
   primera versión: mensajería, lista de espera, geolocalización/mapa y modo offline.
4. Tecnologías fuera del PDF que el proyecto base usa (ver diagnóstico §7): ¿se aprueban, se
   reemplazan o se dejan para después?
5. En el proyecto base los planes de suscripción son globales. Con el diseño multi-tenant, ¿cada
   cadena define sus propios planes?
6. Al migrar se corrigen los permisos que el proyecto base no valida en la API (suscripciones,
   parámetros, métricas y la regla de suscripción activa para reservar). ¿Está bien considerarlo
   parte de la migración y no una mejora?
