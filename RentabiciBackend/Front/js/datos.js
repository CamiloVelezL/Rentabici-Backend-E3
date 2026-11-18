/* RentaBici - Capa de datos: cliente de la API del backend (ASP.NET Core).
 * Antes los repositorios leían y escribían localStorage; ahora cada método llama
 * a la API y devuelve una promesa. Las reglas de negocio (RN-01…RN-06) las aplica
 * el backend: si algo no se puede hacer, el método lanza un ErrorApi con el
 * mensaje en español que se muestra al usuario. */

// Si la página la sirve el propio backend (wwwroot) se usa su misma dirección;
// si se abre con Live Server o como archivo, se apunta al puerto del backend.
const API_URL =
  ["5181", "7132"].includes(window.location.port)
    ? window.location.origin + "/api"
    : "http://localhost:5181/api";

const CLAVE_SESION = "rentabici_sesion";

// Estados posibles de una estación
const ESTADOS_ESTACION = {
  ABIERTA: "Abierta",
  CERRADA: "Cerrada",
};

// Texto que se muestra para cada estado de la bicicleta (máquina de estados del backend)
const ESTADOS_BICICLETA = {
  DISPONIBLE: "Disponible",
  RESERVADA: "Reservada",
  ALQUILADA: "Alquilada",
  REVISION: "En revisión",
  REPARACION: "Reparación",
  BAJA: "Dada de baja",
};

const TEXTO_ESTADO_BICICLETA = {
  Disponible: ESTADOS_BICICLETA.DISPONIBLE,
  Reservada: ESTADOS_BICICLETA.RESERVADA,
  Alquilada: ESTADOS_BICICLETA.ALQUILADA,
  EnRevision: ESTADOS_BICICLETA.REVISION,
  EnMantenimiento: ESTADOS_BICICLETA.REPARACION,
  DadaDeBaja: ESTADOS_BICICLETA.BAJA,
};

// Deben coincidir con "Tarifas" de appsettings.json (el backend es quien cobra;
// aquí solo se usan para mostrar el costo estimado y las opciones de tiempo)
const TARIFAS = {
  PRECIO_LAPSO: 3000,        // COP por cada 30 minutos o fracción
  MINUTOS_LAPSO: 30,
  LIMITE_MINUTOS: 120,       // tiempo límite permitido (2 horas)
  RECARGO_MINUTO: 200,       // COP por cada minuto de demora
  MINUTOS_RESERVA: 15,       // la reserva expira a los 15 minutos (RN-02)
};

function desglosarCosto(minutos) {
  const lapsos = Math.max(1, Math.ceil(minutos / TARIFAS.MINUTOS_LAPSO));
  const demora = Math.max(0, minutos - TARIFAS.LIMITE_MINUTOS);
  return {
    alquiler: lapsos * TARIFAS.PRECIO_LAPSO,
    multa: Math.ceil(demora) * TARIFAS.RECARGO_MINUTO,
  };
}

function calcularCosto(minutos) {
  const { alquiler, multa } = desglosarCosto(minutos);
  return alquiler + multa;
}

function formatearPesos(valor) {
  return "$" + Number(valor).toLocaleString("es-CO");
}

// Identificadores de los roles (RF-02) — iguales al enum Rol del backend
const ROLES = {
  ADMINISTRADOR: 1,
  TECNICO: 2,
  USUARIO: 3,
};

const GRAVEDADES_FALLA = ["Leve", "Intermedia", "Mayor"];

/* ===================== Cliente HTTP ===================== */

class ErrorApi extends Error {
  constructor(status, codigo, mensaje, errores) {
    super(mensaje);
    this.status = status;
    this.codigo = codigo;
    this.errores = errores || null;
  }

  // Mensaje + detalle de cada campo inválido (respuestas 400 VALIDACION)
  get detalle() {
    if (!this.errores) return this.message;
    const lista = Object.values(this.errores).flat();
    return lista.length ? lista.join(" ") : this.message;
  }
}

class ClienteApi {
  token() {
    try {
      const sesion = JSON.parse(sessionStorage.getItem(CLAVE_SESION));
      return sesion ? sesion.token : null;
    } catch (e) {
      return null;
    }
  }

  async solicitar(metodo, ruta, cuerpo) {
    const cabeceras = { Accept: "application/json" };
    const token = this.token();
    if (token) cabeceras.Authorization = "Bearer " + token;
    if (cuerpo !== undefined) cabeceras["Content-Type"] = "application/json";

    let respuesta;
    try {
      respuesta = await fetch(API_URL + ruta, {
        method: metodo,
        headers: cabeceras,
        body: cuerpo !== undefined ? JSON.stringify(cuerpo) : undefined,
      });
    } catch (e) {
      throw new ErrorApi(0, "SIN_CONEXION",
        "No se pudo conectar con el servidor. Verifica que el backend esté en ejecución.");
    }

    const json = await respuesta.json().catch(() => null);

    if (!respuesta.ok) {
      const codigo = (json && json.codigo) || "ERROR";
      // Sesión vencida o inválida: se limpia y se vuelve a iniciar sesión
      if (respuesta.status === 401 && codigo !== "CREDENCIALES_INVALIDAS") {
        sessionStorage.removeItem(CLAVE_SESION);
        window.location.replace("login.html");
      }
      throw new ErrorApi(respuesta.status, codigo,
        (json && json.mensaje) || "Error " + respuesta.status, json && json.errores);
    }
    return json || { mensaje: "", datos: null };
  }

  get(ruta) { return this.solicitar("GET", ruta); }
  post(ruta, cuerpo) { return this.solicitar("POST", ruta, cuerpo === undefined ? {} : cuerpo); }
  put(ruta, cuerpo) { return this.solicitar("PUT", ruta, cuerpo); }
  delete(ruta) { return this.solicitar("DELETE", ruta); }
}

/* Muestra un error de la API en el elemento de mensajes o en un pop-up */
function avisarError(error, elementoMensaje) {
  const texto = error instanceof ErrorApi ? error.detalle : "Ocurrió un error inesperado. Intenta de nuevo.";
  if (elementoMensaje) {
    elementoMensaje.textContent = texto;
    elementoMensaje.className = "mensaje mensaje--error";
  } else if (typeof mostrarAviso === "function") {
    mostrarAviso("No se pudo completar", texto);
  } else {
    alert(texto);
  }
}

/* ===================== Conversión API → pantalla ===================== */

const convertir = {
  usuario: (u) => ({
    idUsuario: u.id, nombre: u.nombre, apellidos: u.apellidos, email: u.email,
    documento: u.documento, idRol: u.idRol, tieneMulta: u.tieneMulta,
    saldoPendiente: u.saldoPendiente, bloqueado: u.bloqueado,
  }),
  estacion: (e) => ({
    idEstacion: e.id, nombre: e.nombre, direccion: e.direccion, estado: e.estado,
    capacidadMaxima: e.capacidad, ocupacion: e.ocupacion, disponibles: e.disponibles,
    espaciosLibres: e.espaciosLibres,
  }),
  bicicleta: (b) => ({
    idBicicleta: b.id, codigo: b.codigo, modelo: b.modelo, idEstacionActual: b.estacionId,
    estado: TEXTO_ESTADO_BICICLETA[b.estado] || b.estado, estadoApi: b.estado,
  }),
  reserva: (r) => ({
    idReserva: r.id, idUsuario: r.usuarioId, idBicicleta: r.bicicletaId, idEstacion: r.estacionId,
    modelo: r.modelo, fechaUso: r.fechaUso, telefono: r.telefono, tiempoMinutos: r.tiempoMinutos,
    costoEstimado: r.costoEstimado, fechaReserva: r.creada, expira: r.expira, estadoReserva: r.estado,
  }),
  alquiler: (a) => ({
    idAlquiler: a.id, idUsuario: a.usuarioId, idBicicleta: a.bicicletaId,
    idEstacionOrigen: a.estacionOrigenId, idEstacionDestino: a.estacionDestinoId,
    fechaHoraInicio: a.inicio, fechaHoraFin: a.fin, tiempoMinutos: a.tiempoReservado,
    minutosUsados: a.minutosUso, montoAlquiler: a.montoAlquiler, montoMulta: a.montoRecargo,
    tarifaTotal: a.costo, tarifaAplicada: a.tarifaAplicada,
  }),
  incidencia: (i) => ({
    idIncidencia: i.id, idBicicleta: i.bicicletaId, idUsuarioReporta: i.usuarioId,
    idEstacion: i.estacionId, descripcionFalla: i.gravedad, detalle: i.descripcion,
    fechaReporte: i.fecha, estadoIncidencia: i.estado,
  }),
};

/* ===================== Repositorios (clases del diagrama) ===================== */

class RepositorioRoles {
  constructor(api) { this.api = api; }

  async listar() {
    return (await this.api.get("/roles")).datos; // [{ idRol, nombreRol, descripcion }]
  }

  // Nombre del rol sin llamar a la API (para el saludo del menú)
  nombre(idRol) {
    return { 1: "Administrador", 2: "Técnico", 3: "Usuario" }[idRol] || "";
  }
}

class RepositorioUsuarios {
  constructor(api) { this.api = api; }

  async listar() {
    return (await this.api.get("/usuarios")).datos.map(convertir.usuario);
  }

  async cambiarRol(idUsuario, idRol) {
    return convertir.usuario((await this.api.put("/usuarios/" + idUsuario + "/rol", { idRol })).datos);
  }

  async actual() {
    return convertir.usuario((await this.api.get("/auth/me")).datos);
  }
}

class RepositorioEstaciones {
  constructor(api) { this.api = api; }

  async listar() {
    return (await this.api.get("/estaciones")).datos.map(convertir.estacion);
  }

  async agregar({ nombre, direccion, capacidadMaxima }) {
    return convertir.estacion((await this.api.post("/estaciones", { nombre, direccion, capacidad: capacidadMaxima })).datos);
  }

  async cambiarEstado(idEstacion, estado) {
    return convertir.estacion((await this.api.put("/estaciones/" + idEstacion + "/estado", { estado })).datos);
  }
}

class RepositorioBicicletas {
  constructor(api) { this.api = api; }

  async listar() {
    return (await this.api.get("/bicicletas")).datos.map(convertir.bicicleta);
  }

  async agregar({ modelo, idEstacionActual }) {
    return convertir.bicicleta((await this.api.post("/bicicletas", { modelo, estacionId: idEstacionActual })).datos);
  }
}

class RepositorioReservas {
  constructor(api) { this.api = api; }

  // El backend expira las reservas vencidas (RN-02) antes de responder
  async buscarActivaDeUsuario() {
    const datos = (await this.api.get("/reservas/activa")).datos;
    return datos ? convertir.reserva(datos) : null;
  }

  // El backend elige la bicicleta disponible del modelo en la estación y valida RN-01 / RN-04
  async agregar({ idEstacion, modelo, fechaUso, telefono, tiempoMinutos }) {
    const r = await this.api.post("/reservas", { estacionId: idEstacion, modelo, fechaUso, telefono, tiempoMinutos });
    return convertir.reserva(r.datos);
  }

  async cancelar(idReserva) {
    return convertir.reserva((await this.api.delete("/reservas/" + idReserva)).datos);
  }
}

class RepositorioAlquileres {
  constructor(api) { this.api = api; }

  // Alquiler sin terminar (todavía no se entrega la bicicleta)
  async buscarEnCursoDeUsuario() {
    const datos = (await this.api.get("/alquileres/activo")).datos;
    return datos ? convertir.alquiler(datos) : null;
  }

  // RF-06: recibir la bicicleta reservada
  async iniciarViaje(reserva) {
    return convertir.alquiler((await this.api.post("/alquileres", { bicicletaId: reserva.idBicicleta })).datos);
  }

  // RF-07: entregar la bicicleta (por defecto en la estación de origen)
  async finalizarViaje(idAlquiler, idEstacionDestino) {
    const cuerpo = idEstacionDestino ? { estacionDestinoId: idEstacionDestino } : {};
    return convertir.alquiler((await this.api.post("/alquileres/" + idAlquiler + "/devolucion", cuerpo)).datos);
  }
}

class RepositorioPagos {
  constructor(api) { this.api = api; }

  // RF-10 / RN-04: filas "Alquiler" y "Multa" pendientes, total y código de la factura
  async pendientes() {
    return (await this.api.get("/pagos/pendientes")).datos; // { items: [{ tipo, monto }], total, codigoFactura }
  }

  // Paga la factura con su código (RB-0000). Devuelve { totalPagado, codigoFactura, comprobante }
  async pagarFactura(codigoFactura) {
    return (await this.api.post("/pagos", { codigoFactura })).datos;
  }
}

class RepositorioIncidencias {
  constructor(api) { this.api = api; }

  async pendientes() {
    return (await this.api.get("/incidencias?estado=Pendiente")).datos.map(convertir.incidencia);
  }

  // Devuelve { incidencia, mensaje } (el mensaje explica en qué estado quedó la bicicleta)
  async crearReporte({ idBicicleta, idEstacion, gravedad }) {
    const r = await this.api.post("/incidencias", { bicicletaId: idBicicleta, estacionId: idEstacion, gravedad });
    return { incidencia: convertir.incidencia(r.datos), mensaje: r.mensaje };
  }

  // RF-09: devuelve { incidencia, habilitada }
  async marcarAtendida(idIncidencia) {
    const r = await this.api.post("/incidencias/" + idIncidencia + "/atender", {});
    return { incidencia: convertir.incidencia(r.datos.incidencia), habilitada: r.datos.bicicletaHabilitada };
  }

  // RF-09: la bicicleta no tiene reparación → pasa al estado final "Dada de baja"
  async darDeBaja(idIncidencia) {
    const r = await this.api.post("/incidencias/" + idIncidencia + "/baja", {});
    return { incidencia: convertir.incidencia(r.datos.incidencia) };
  }
}

/* Instancias compartidas por todas las pantallas */
const clienteApi = new ClienteApi();
const repositorioRoles = new RepositorioRoles(clienteApi);
const repositorioUsuarios = new RepositorioUsuarios(clienteApi);
const repositorioEstaciones = new RepositorioEstaciones(clienteApi);
const repositorioBicicletas = new RepositorioBicicletas(clienteApi);
const repositorioReservas = new RepositorioReservas(clienteApi);
const repositorioAlquileres = new RepositorioAlquileres(clienteApi);
const repositorioPagos = new RepositorioPagos(clienteApi);
const repositorioIncidencias = new RepositorioIncidencias(clienteApi);

// Limpia los datos que guardaba la versión anterior (solo localStorage)
["rentabici_roles", "rentabici_usuarios", "rentabici_estaciones", "rentabici_bicicletas",
 "rentabici_reservas", "rentabici_alquileres", "rentabici_pagos", "rentabici_incidencias",
 "rentabici_mantenimientos", "rentabici_version", "rentabici_fallas"].forEach((clave) => {
  try { localStorage.removeItem(clave); } catch (e) { /* sin almacenamiento */ }
});
