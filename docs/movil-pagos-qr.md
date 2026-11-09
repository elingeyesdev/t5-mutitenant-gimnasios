# Renovación mediante QR desde la aplicación móvil

## Identificación

- **Issue:** #61
- **Requisito funcional:** RF-05 · Renovación de suscripciones mediante pago QR y comprobantes.
- **Historia de usuario:** HU-50 · Renovar la suscripción mediante QR.
- **Caso de uso:** CU-39 · Renovar suscripción con QR.
- **EDT:** Actividad L · 6.2 · Flujo de pago QR y comprobantes en la app móvil.

## Objetivo y navegación

La funcionalidad permite que el socio genere una orden de renovación, consulte el QR demostrativo,
registre la referencia de su pago y adjunte opcionalmente una imagen del comprobante. La suscripción
no se renueva en el momento de crear la orden: recepción debe revisar el comprobante y confirmar la
orden desde el panel web.

El acceso está disponible para socios en:

**Más → Renovar con QR**

La opción se agrega desde `movil/GestionGimnasios.Movil/Paginas/Comun/MasPage.cs`.

## Pantallas y flujo implementado

### `PagosQrPage`

La pantalla consulta las suscripciones del socio y muestra las que no están canceladas. Para cada una
permite seleccionar una duración de **1 a 12 meses** y generar una orden. También muestra el historial
de órdenes con su monto en bolivianos, código, cantidad de meses, fechas, vencimiento y estado.

Cuando no existen suscripciones u órdenes, muestra los mensajes de estado vacío correspondientes.
La pantalla se actualiza al volver a aparecer y mediante el gesto de refresco manual.

### `PagoQrDetallePage`

El detalle consulta nuevamente la orden y muestra:

- código de la orden y monto en bolivianos;
- estado `PENDIENTE`, `CONFIRMADA`, `RECHAZADA` o `VENCIDA`;
- fecha de creación, vencimiento de la orden y vencimiento de la suscripción;
- QR recibido en Base64 mientras la orden está pendiente;
- referencia obligatoria del comprobante;
- imagen opcional del comprobante;
- motivo de rechazo cuando la API lo informa.

La imagen puede ser JPG, JPEG, PNG o WEBP y no debe superar los 5 MB. Mientras se procesa una
solicitud, `PaginaBase` y el botón de envío evitan solicitudes repetidas. Los errores devueltos por
la API se muestran mediante el manejo común de `PaginaBase`.

El QR es demostrativo: identifica la orden, pero no realiza un cobro bancario real. La membresía solo
se renueva después de que recepción verifica el pago y confirma la orden.

## Integración con el backend

La aplicación utiliza `Sesion.Api`, que agrega el token JWT de la sesión. Los contratos están definidos
en `web/GestionGimnasios.Web/Services/PagosQrService.cs` y las rutas en
`web/GestionGimnasios.Web/Controllers/Api/PagosQrApi.cs`.

| Método y ruta | Solicitud | Respuesta utilizada |
|---|---|---|
| `GET /api/suscripciones/mias` | Sin cuerpo | Lista de suscripciones del socio, incluyendo `Id`, `Plan`, `PrecioMensual`, `Estado`, `Inicio`, `Fin` y `DiasRestantes`. |
| `POST /api/pagos-qr` | JSON con `suscripcionId` y `meses` | `OrdenPagoDto` con código, monto, estado, fechas, vencimiento de suscripción y, mientras está pendiente, `QrPngBase64`. |
| `GET /api/pagos-qr/mias` | Sin cuerpo | Historial de `OrdenPagoDto` del socio. |
| `GET /api/pagos-qr/{id}` | Identificador de la orden en la ruta | Detalle de la orden propia; incluye el QR Base64 cuando corresponde. |
| `POST /api/pagos-qr/{id}/comprobante` | `multipart/form-data` con `Referencia` obligatoria y `Archivo` opcional | Orden actualizada con los datos del comprobante. |

El endpoint de creación valida que la suscripción pertenezca al socio, que no esté cancelada, que su
plan esté activo y que no exista otra orden pendiente para la misma suscripción. El backend también
valida la referencia, el formato y el tamaño de la imagen, y vence las órdenes pendientes sin
comprobante después de 24 horas. La aplicación no reproduce estas reglas: muestra los mensajes de la
API.

## Archivos involucrados

- `movil/GestionGimnasios.Movil/Paginas/Cliente/PagosQrPage.cs`: consulta suscripciones e historial,
  selecciona meses y crea órdenes.
- `movil/GestionGimnasios.Movil/Paginas/Cliente/PagoQrDetallePage.cs`: muestra el detalle, QR,
  referencia y carga opcional del comprobante.
- `movil/GestionGimnasios.Movil/Paginas/Comun/MasPage.cs`: agrega el acceso para socios.
- `movil/GestionGimnasios.Movil/Modelos/Modelos.cs`: contiene `OrdenPagoQr` y sus textos de pantalla.
- `movil/GestionGimnasios.Movil/Servicios/Api.cs`: envía formularios multipart con referencia y archivo.
- `web/GestionGimnasios.Web/Controllers/PagosQrController.cs`: permite a recepción listar, revisar,
  confirmar o rechazar órdenes desde el panel.
- `web/GestionGimnasios.Web/Views/PagosQr/Index.cshtml` y `Detalle.cshtml`: vistas web de revisión y
  conciliación.

## Prueba manual reproducible en Android y panel web

1. Iniciar sesión en Android con un socio que tenga una suscripción activa y plan vigente.
2. Abrir **Más → Renovar con QR**.
3. Seleccionar la suscripción, elegir un mes y pulsar **Generar orden QR**.
4. Abrir el detalle y comprobar el monto, código, QR demostrativo, estado pendiente y vencimiento.
5. Escribir una referencia válida y registrar el comprobante sin adjuntar imagen.
6. En el panel web, abrir **Pagos QR**, localizar la orden pendiente y revisar la referencia.
7. Confirmar la orden desde recepción.
8. Volver a Android, deslizar para refrescar y comprobar el nuevo estado y vencimiento de la
   suscripción.

## Resultado de la prueba manual

Se comprobó el flujo con una orden de **Bs 250 por un mes**. El socio registró la referencia sin
adjuntar imagen; recepción confirmó la orden desde el panel web. Al refrescar Android, la orden mostró
estado **Confirmada** y el vencimiento de la suscripción se actualizó del **15/11/2026** al
**15/12/2026**.
