# RentaBici – Sistema de alquiler de bicicletas (MVP · Entregable 3)

Sistema web para consultar estaciones, reservar, recibir, entregar y pagar bicicletas,
reportar fallas y gestionar estaciones, flota, roles y mantenimiento.

**Curso:** Diseño de Sistemas de Información 580304012-8 – Instituto Tecnológico Metropolitano
**Docente:** Alexandra Guerrero Bocanegra · **Equipo 4**

## Integrantes

| Camilo Andrés Vélez Luna |

| Samuel Castro Bedoya |

| Davidson Osorio Noreña | 

| Edison Stiven López Galeano | 

| Eloisa Ospina Isaza| 

## Tecnologías

| Capa | Tecnología |
|---|---|
| Frontend | HTML5, CSS3, JavaScript (fetch API) – carpeta `RentabiciBackend/Front` |
| Backend | ASP.NET Core Web API 9 (C#) – carpeta `RentabiciBackend` |
| Documentación de la API | Swagger (Swashbuckle) |
| Persistencia | Archivo JSON `RentabiciBackend/App_Data/rentabici-datos.json` detrás de `IPersistenciaDatos`. SQL Server = **elemento proyectado** |
| Recaudo de pagos, correo y SMS | Simulados. Punto de pago/PSE, SendGrid y Twilio = **elementos proyectados** |

## Arquitectura

```
Navegador ──HTTP/JSON + token Bearer──►  API ASP.NET Core (http://localhost:5181/api)
(HTML/CSS/JS)                             ├─ Middleware   ErrorHandlingMiddleware (errores JSON) · CORS
 Front/js/datos.js = cliente API          ├─ Security     AutorizarAttribute (roles), SessionService, PBKDF2
                                          ├─ Controllers  Auth, Station, Rental, Payment, Incidence, Notification
                                          ├─ Services     reglas de negocio + patrones GoF
                                          ├─ Domain       máquina de estados de la Bicicleta (State)
                                          └─ Data         MemoryDatabase + IPersistenciaDatos (archivo JSON; SQL Server proyectado)
```

### Patrones GoF implementados

| Patrón | Archivo | Uso en el MVP |
|---|---|---|
| **State** | `Domain/Estados/BikeStates.cs`, `Models/Bicicleta.cs` | Ciclo de vida de la bicicleta. Una acción no permitida en el estado actual → `TRANSICION_INVALIDA` |
| **Strategy** | `Services/Tarifas/Tarifas.cs` | Tarifa estándar (RN-06) o tarifa con recargo por demora (RF-11) |
| **Proxy** | `Services/RentalServiceProxy.cs` | Antes de alquilar valida multa, saldo (RN-04) y una bici a la vez (RN-01). No bloquea devoluciones |
| **Facade** | `Services/Pagos/Pagos.cs` (`PaymentFacade`) | Oculta el sistema de recaudo externo, su posible caída y el envío del comprobante |
| **Factory Method** | `Services/Notificaciones/Notificaciones.cs` | Crea notificaciones Email/SMS sin acoplar el negocio al canal |

### Principios SOLID en el código

- **SRP:** un servicio por responsabilidad (`ReservaService`, `PagoService`, `IncidenciaService`…).
- **OCP:** nuevos estados o tarifas = nuevas clases, sin modificar las existentes.
- **LSP:** `RentalServiceProxy` y `RentalManagerService` son intercambiables mediante `IRentalService`.
- **ISP:** `IReportadorIncidencias` (usuarios) e `IGestorMantenimiento` (técnicos), implementadas por la misma clase; `IAlquilerConsultaService` separado de `IRentalService`.
- **DIP:** controladores y servicios dependen de interfaces registradas en `Program.cs` (`IPersistenciaDatos`, `ISistemaRecaudo`, `INotificacionService`, `ITarifaSelector`…).

### Máquina de estados de la Bicicleta (implementada en `BikeStates.cs`)

| Estado | Evento → estado destino |
|---|---|
| Disponible | Reservar → Reservada · Alquilar → Alquilada · ReportarFalla → EnRevision / [Mayor o RN-05] EnMantenimiento |
| Reservada | Alquilar (recibir) → Alquilada · CancelarReserva / expira RN-02 → Disponible · ReportarFalla → EnRevision / EnMantenimiento |
| Alquilada | Devolver → Disponible / [falla en viaje] EnRevision / [falla Mayor] EnMantenimiento · ReportarFalla → Alquilada (falla pendiente) |
| EnRevision | Liberar (falla solucionada) → Disponible · ReportarFalla [Mayor o RN-05] → EnMantenimiento · DarDeBaja → DadaDeBaja |
| EnMantenimiento | Liberar (reparada) → Disponible · DarDeBaja (no reparable) → DadaDeBaja |
| DadaDeBaja | Estado final (no acepta eventos) |

## Implementado vs. proyectado (coherencia con el Entregable 2)

| Elemento del E2 | En el MVP (E3) | Dónde está en el código |
|---|---|---|
| C4 N2 – App web/móvil (Blazor WASM / React) | Implementado con **HTML, CSS y JavaScript**, servido por el mismo backend | `RentabiciBackend/Front` |
| C4 N2 – Panel web de administración (Blazor Server / MVC) | Integrado en la misma app web: menús y pantallas por rol | `Front/menu_administrador.html`, `Front/menu_tecnico.html` |
| C4 N2 – API ASP.NET Core | **Implementado** | `Program.cs`, `Controllers/` |
| C4 N2 – Base de datos SQL Server | **Proyectado.** MVP: archivo JSON detrás de `IPersistenciaDatos` | `Data/Persistencia.cs` |
| C4 N3 – AuthController | **Implementado** (registro, login, roles) | `Controllers/AuthController.cs` |
| C4 N3 – RentalController | **Implementado** (reserva, recibir, entregar) | `Controllers/RentalController.cs` |
| C4 N3 – StationController | **Implementado** (estaciones y flota) | `Controllers/StationController.cs` |
| C4 N3 – PaymentController + pasarela Wompi/Stripe | Implementado con **pago por código de factura**; la pasarela real es **proyectada** (`RecaudoSimulado`) | `Controllers/PaymentController.cs`, `Services/Pagos/Pagos.cs` |
| C4 N3 – IncidenceController | **Implementado** (reportar, solucionar, dar de baja) | `Controllers/IncidenceController.cs` |
| C4 N4 – `IPasswordHasher` (BCrypt) | Implementado como `PasswordHasher` con **PBKDF2-SHA256** (incluido en .NET) | `Security/Seguridad.cs` |
| C4 N4 – `IJwtTokenGenerator` (JWT) | **Proyectado.** MVP: token Bearer opaco con `ISessionService` (se reemplaza por JWT sin tocar controladores) | `Security/Seguridad.cs` |
| C4 N4 – Repositorios genéricos | **Proyectado.** MVP: `MemoryDatabase` + `IPersistenciaDatos` | `Data/` |
| State – `AvailableState`, `RentedState`, `MaintenanceState` | **Implementado** como `DisponibleState`, `ReservadaState`, `AlquiladaState`, `EnRevisionState`, `EnMantenimientoState`, `DadaDeBajaState`. "Bloqueada" se reemplazó por "En revisión" y "Dada de baja" | `Domain/Estados/BikeStates.cs` |
| State – ciclo del Alquiler (Activo/Finalizado/Cancelado) | Implementado como enumeración `EstadoAlquiler` (Activo, PendientePago, Pagado), sin clases State | `Models/Enums.cs` |
| Strategy – `StandardTariff`, `PenaltyTariff` | **Implementado** (`TarifaEstandarStrategy`, `TarifaConRecargoStrategy`) | `Services/Tarifas/Tarifas.cs` |
| Strategy – `MembershipTariff` | **Proyectado** (el MVP no tiene membresías) | — |
| Facade – `PaymentFacade` | **Implementado** (oculta el sistema de recaudo y el comprobante) | `Services/Pagos/Pagos.cs` |
| Facade – `IoTDeviceFacade` (candados inteligentes) | **Proyectado.** MVP: "Recibir bicicleta" con QR simulado | `Front/recibir_qr.html` |
| Factory Method – Email / SMS | **Implementado** con envío simulado (registro en notificaciones) | `Services/Notificaciones/Notificaciones.cs` |
| Factory Method – Push (Firebase), SendGrid, Twilio | **Proyectados** | — |
| Proxy – `RentalServiceProxy` | **Implementado** | `Services/RentalServiceProxy.cs` |
| SOLID SRP – clases Alquiler y Pago separadas | **Implementado** | `Models/Alquiler.cs`, `Models/Pago.cs` |
| SOLID OCP – `IMetodoPago` (PagoTarjeta, PagoPSE) | **Proyectado.** En el MVP OCP se evidencia con `ITarifaStrategy` e `IBikeState` | `Services/Tarifas/`, `Domain/Estados/` |
| SOLID LSP – jerarquía Usuario / Administrador / Técnico | **Proyectado.** MVP: el rol es un atributo (`Rol`); LSP se evidencia con `IRentalService` (Proxy y servicio real intercambiables) | `Services/IRentalService.cs` |
| SOLID ISP – `IReportadorIncidencias` / `IGestorMantenimiento` | **Implementado** | `Services/OperacionesService.cs` |
| SOLID DIP – `IRepositorioAlquileres`, `IPasarelaPago` | Implementado como `IPersistenciaDatos` e `ISistemaRecaudo` | `Data/Persistencia.cs`, `Services/Pagos/Pagos.cs` |
| RF-12 – recibo en PDF por correo | **Proyectado.** MVP: comprobante en texto como notificación | `Services/Pagos/Pagos.cs` |
| Épica 5 – Analítica y reportes | **Proyectado** (fuera del alcance del MVP) | — |
| RNF-03 – Disponibilidad 99.5 % | **Proyectado** (requiere despliegue en la nube) | — |

## Funcionalidades implementadas

| Épica | Pantalla del frontend | Endpoint | Requisitos |
|---|---|---|---|
| E1 | Registro | `POST /api/auth/registro` | RF-01, RNF-02 |
| E1 | Inicio de sesión / menús por rol | `POST /api/auth/login` | RF-02 |
| E1 | Gestión de roles y permisos | `GET /api/usuarios`, `PUT /api/usuarios/{id}/rol` | RF-02 |
| E2 | Consulta de estaciones | `GET /api/estaciones`, `GET /api/bicicletas` | RU-02 |
| E2 | Control y agregar estaciones | `POST /api/estaciones`, `PUT /api/estaciones/{id}/estado` | RF-03 |
| E2 | Control y agregar bicicletas | `POST /api/bicicletas` | RF-04, RN-03 |
| E3 | Alquiler (pasos 1 y 2) = reserva | `POST /api/reservas` | RF-05, RN-01, RN-02, RN-04 |
| E3 | Administrar reservas: recibir (QR), entregar, cancelar | `POST /api/alquileres`, `POST /api/alquileres/{id}/devolucion`, `DELETE /api/reservas/{id}` | RF-06, RF-07, RN-03 |
| E4 | Reporte de fallas | `POST /api/incidencias` | RF-08, RN-05 |
| E4 | Fallas reportadas (solucionar o dar de baja) | `GET /api/incidencias?estado=Pendiente`, `POST /api/incidencias/{id}/atender`, `POST /api/incidencias/{id}/baja` | RF-09 |
| E5 | Pagos y multas (factura con código) / pagar con código de factura | `GET /api/pagos/pendientes`, `POST /api/pagos` | RF-10, RF-11, RF-12, RN-04, RN-06 |

### Flujos alternativos, validaciones y excepciones

Todos los errores tienen el formato `{ "codigo": "...", "mensaje": "...", "errores": { campo: [..] } }`
y el frontend muestra el `mensaje` en la pantalla.

| Código | HTTP | Cuándo ocurre |
|---|---|---|
| `VALIDACION` | 400 | Datos inválidos: documento, correo, teléfono, fecha inexistente o pasada, código de factura con formato incorrecto… |
| `CREDENCIALES_INVALIDAS`, `NO_AUTENTICADO`, `SESION_INVALIDA` | 401 | Login fallido, sin token o token vencido (el front vuelve al login) |
| `ACCESO_DENEGADO` | 403 | Rol sin permiso o recurso de otro usuario |
| `USUARIO_CON_MULTA`, `USUARIO_CON_SALDO` | 403 | RN-04 |
| `NO_ENCONTRADO`, `BICICLETA_NO_EXISTE`, `FACTURA_NO_EXISTE` | 404 | Recurso inexistente |
| `LIMITE_ACTIVO` | 409 | RN-01: ya tiene una reserva o viaje |
| `ESTACION_LLENA` | 409 | RN-03 (sugiere estaciones con espacio) |
| `ESTACION_CERRADA`, `ESTACION_CON_RESERVAS` | 409 | Reservar/entregar en estación cerrada, o cerrar una estación con reservas |
| `SIN_BICICLETAS` | 409 | Otro usuario tomó la última bicicleta del modelo |
| `TRANSICION_INVALIDA` | 409 | Acción no permitida en el estado actual de la bicicleta (State) |
| `BICICLETA_NO_ESTA_EN_ESTACION`, `REPORTE_DUPLICADO`, `INCIDENCIA_YA_ATENDIDA`, `BICICLETA_EN_USO` | 409 | Reglas de incidencias |
| `FACTURA_YA_PAGADA` | 409 | Se intenta pagar de nuevo una factura |
| `EMAIL_DUPLICADO`, `DOCUMENTO_DUPLICADO`, `ESTACION_DUPLICADA`, `CAMBIO_PROPIO_ROL`, `ULTIMO_ADMINISTRADOR` | 409 | Otras reglas |
| `PAGO_NO_PROCESADO` | 503 | El sistema de recaudo no responde |
| `SIN_CONEXION` | – | (frontend) el backend no está en ejecución |
| `ERROR_INTERNO` | 500 | Error no controlado, sin exponer detalles |

El archivo `RentabiciBackend/RentabiciBackend.http` tiene una petición por cada flujo principal (FP) y alternativo (FA).

## Requisitos de instalación

- [.NET SDK 9.0](https://dotnet.microsoft.com/download) (`dotnet --version`)
- VS Code con las extensiones **Live Server** y **REST Client** (o Visual Studio 2022)

## Ejecución

```bash
cd RentabiciBackend
dotnet restore
dotnet run --launch-profile http
```

El backend sirve también el frontend (carpeta `RentabiciBackend/Front`), así que basta con abrir:

- Aplicación (frontend): http://localhost:5181 (se abre sola al ejecutar)
- API: http://localhost:5181/api
- Swagger: http://localhost:5181/swagger (botón *Authorize* → pegar el token del login)

> También se puede abrir `Front/index.html` con **Live Server**: `Front/js/datos.js` apunta a `http://localhost:5181/api`
> y el backend tiene CORS habilitado. El backend debe estar en ejecución en ambos casos.

### Usuarios de prueba (contraseña `12345`)

| Usuario | Rol | Particularidad |
|---|---|---|
| sam | Administrador | |
| sam2 | Usuario | |
| sam3 | Técnico | |
| pedro | Usuario | Tiene multa (RN-04) y un viaje en curso con la bicicleta 0004 |

Datos de demostración: la Estación 4 está cerrada; la bicicleta 0002 está en reparación y la 0007
en revisión con un reporte (un segundo reporte de otro usuario la envía a reparación, RN-05).
Pago: en "Pagos y multas" aparece el código de la factura (p. ej. `RB-0001`); se escribe en la pantalla de pago y se confirma.
Los datos se guardan en `RentabiciBackend/App_Data/rentabici-datos.json` y se conservan entre ejecuciones.
Para volver a los datos de prueba: detener el backend, borrar ese archivo y ejecutarlo de nuevo.

## Documentación y diagramas

- Prototipo Figma: https://www.figma.com/design/3KfITtq3agzAEP8P7aLblq/Untitled
- Historias de usuario (Jira): https://osoriodavidson33.atlassian.net/jira/software/projects/SCRUM/boards/1
- Arquitectura C4: _enlace Lucid_
- Máquina de estados: _completar_
- Diagramas de componentes y despliegue: _completar_
- Diagramas de secuencia: _completar_

## Evidencias del MVP

_Agregar capturas en `docs/evidencias/` y enlazarlas aquí._

## Uso de inteligencia artificial

_Declarar las herramientas de IA utilizadas y en qué partes, como lo exige el Entregable 3._
