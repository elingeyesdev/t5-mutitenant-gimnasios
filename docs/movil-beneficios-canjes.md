# Beneficios y canjes desde la aplicación móvil

## Identificación

- **Issue:** #57
- **Requisito funcional:** RF-03 · Recompensas mediante vales, cupones y reglas verificables de canje.
- **Historia de usuario:** HU-48 · Recompensas y cupones.
- **Caso de uso:** CU-37 · Obtener y canjear recompensas.
- **EDT:** Actividad H · 4.2 · RF-03: visualización y canje desde la app.

## Objetivo y navegación

La pantalla permite que el socio consulte los beneficios disponibles para sus cadenas, revise sus
condiciones y canjee los beneficios para los que es elegible. Después del canje se muestra el código
que debe presentar en recepción.

El acceso está disponible para socios en:

**Más → Beneficios y canjes**

La opción se agrega desde `movil/GestionGimnasios.Movil/Paginas/Comun/MasPage.cs` y abre la pantalla
`BeneficiosPage`.

## Funcionalidades implementadas

La pantalla muestra, para cada beneficio:

- nombre, cadena y tipo de beneficio;
- descripción, vigencia y condiciones;
- reglas de uso, límite por socio y canjes realizados;
- cupos restantes cuando el beneficio tiene un límite total;
- estado de elegibilidad y el motivo cuando no se puede canjear.

Los beneficios elegibles muestran la acción **Canjear beneficio**. Antes de enviarla, la aplicación
solicita confirmación. Mientras se procesa el canje, el botón se deshabilita y `PaginaBase` evita
otra solicitud simultánea desde la pantalla.

La validación definitiva de vigencia, límites, cupos, plan y asistencias la realiza el backend. Su
transacción serializable evita que solicitudes simultáneas superen los límites o registren canjes
duplicados. Después de un canje exitoso, la aplicación muestra el código recibido y actualiza la
lista de beneficios y su elegibilidad.

También se muestra el historial de canjes del socio con:

- beneficio, cadena y tipo;
- código del comprobante;
- fecha del canje;
- estado `EMITIDO` o `ENTREGADO`;
- fecha de entrega cuando la API la informa.

Cuando no existen beneficios o canjes, se muestra un mensaje de estado vacío. La pantalla utiliza el
refresco manual común de las páginas de tarjetas.

## Integración con el backend

La pantalla utiliza `Sesion.Api`, que envía las solicitudes autenticadas con el token JWT de la
sesión. Las rutas pertenecen a `api/beneficios` y usan los contratos definidos en
`web/GestionGimnasios.Web/Services/BeneficiosService.cs`.

| Método y ruta | Solicitud | Respuesta |
|---|---|---|
| `GET /api/beneficios/disponibles` | Sin cuerpo | Lista de beneficios con `Id`, `Cadena`, `Nombre`, `Descripcion`, `Tipo`, `Condiciones`, `VigenteDesde`, `VigenteHasta`, `Reglas`, `CanjesUsados`, `CuposRestantes`, `Elegible` y `Motivo`. |
| `POST /api/beneficios/{id}/canjear` | Sin cuerpo; el identificador va en la ruta | Canje registrado con `Id`, `Codigo`, `BeneficioId`, `Beneficio`, `Tipo`, `UsuarioId`, `Socio`, `Cadena`, `CanjeadoEn`, `Estado` y `EntregadoEn`. |
| `GET /api/beneficios/canjes/mios` | Sin cuerpo | Lista de canjes del socio con la misma estructura de `CanjeDto`. |

La aplicación no reproduce las reglas de elegibilidad del servidor ni utiliza las rutas
administrativas de beneficios.

## Archivos móviles involucrados

- `movil/GestionGimnasios.Movil/Paginas/Cliente/BeneficiosPage.cs`: consulta, visualización,
  confirmación, canje, código de comprobante, historial y actualización manual.
- `movil/GestionGimnasios.Movil/Paginas/Comun/MasPage.cs`: acceso para socios desde el menú Más.
- `movil/GestionGimnasios.Movil/Modelos/Modelos.cs`: modelos `BeneficioDisponible` y
  `CanjeBeneficio`, alineados con `BeneficioSocioDto` y `CanjeDto`.
- `movil/GestionGimnasios.Movil/Paginas/PaginaTarjetas.cs`: contenedor con desplazamiento y refresco
  manual reutilizado por la pantalla.
- `movil/GestionGimnasios.Movil/Servicios/Api.cs`: cliente HTTP autenticado utilizado por la app.

## Prueba manual en Android

La prueba se realizó en el emulador Android API 33 con un socio de Premier:

1. Abrir **Más → Beneficios y canjes**.
2. Verificar el beneficio de Premier **10% de descuento en cafetería**.
3. Confirmar que se muestran su vigencia, condiciones, elegibilidad y cupos.
4. Confirmar el canje.
5. Verificar que el canje se registra correctamente y que el código generado aparece en el
   historial.
6. Comprobar que los cupos restantes se actualizan de **5 a 4**.
7. Confirmar que el socio deja de ser elegible después de su primer canje.
8. Verificar que el estado inicial del canje es `EMITIDO`.

La compilación Android fue exitosa, sin errores ni advertencias. El flujo principal de consulta,
canje e historial fue comprobado manualmente en el emulador Android API 33.
