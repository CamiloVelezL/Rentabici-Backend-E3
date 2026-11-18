/* RentaBici - Fallas Reportadas (Técnico y Administrador, RF-09)
 * Cada falla se puede marcar como "Solucionado" (la bici vuelve a Disponible si no tiene
 * otras fallas) o "Dar de baja" (la bici no tiene reparación: estado final DadaDeBaja). */

const SOLUCIONADO = "Solucionado";
const DAR_DE_BAJA = "Dar de baja"; // la bicicleta no tiene reparación (estado final)

document.addEventListener("DOMContentLoaded", async () => {
  const usuario = servicioSesion.exigirRol([ROLES.TECNICO, ROLES.ADMINISTRADOR]);
  if (!usuario) return; // fue redirigido

  const cuerpo = document.getElementById("cuerpo-tabla");
  const mensaje = document.getElementById("mensaje");
  const botonGuardar = document.getElementById("guardar");
  const enlaceVolver = document.getElementById("volver");
  enlaceVolver.href = servicioSesion.rutaMenu(usuario.idRol);

  const idBicicleta = (incidencia) => String(incidencia.idBicicleta).padStart(4, "0");

  function crearCelda(texto) {
    const celda = document.createElement("td");
    celda.textContent = texto;
    return celda;
  }

  function crearCeldaFalla(incidencia, fila) {
    const celda = document.createElement("td");
    const selector = document.createElement("select");
    selector.className = "selector-rol";
    selector.dataset.idIncidencia = incidencia.idIncidencia;
    selector.setAttribute("aria-label", "Falla de la bicicleta " + idBicicleta(incidencia));

    const gravedad = incidencia.descripcionFalla || "Falla";
    selector.dataset.original = gravedad;
    [gravedad, SOLUCIONADO, DAR_DE_BAJA].forEach((texto) => {
      const opcion = document.createElement("option");
      opcion.value = texto;
      opcion.textContent = texto;
      selector.appendChild(opcion);
    });

    selector.addEventListener("change", () => {
      fila.classList.toggle("fila--cambiada", selector.value !== selector.dataset.original);
      mensaje.textContent = "";
    });

    celda.appendChild(selector);
    return celda;
  }

  // Dibuja la tabla leyendo SIEMPRE lo que hay guardado en el backend
  async function pintarTabla() {
    cuerpo.replaceChildren();
    let estaciones = [];
    let pendientes = [];
    try {
      [estaciones, pendientes] = await Promise.all([repositorioEstaciones.listar(), repositorioIncidencias.pendientes()]);
    } catch (error) {
      avisarError(error, mensaje);
      return;
    }

    pendientes.forEach((incidencia) => {
      const estacion = estaciones.find((e) => e.idEstacion === incidencia.idEstacion);
      const fila = document.createElement("tr");
      fila.appendChild(crearCelda(idBicicleta(incidencia)));
      fila.appendChild(crearCelda(estacion ? estacion.nombre : "-"));
      fila.appendChild(crearCeldaFalla(incidencia, fila));
      cuerpo.appendChild(fila);
    });

    for (let i = pendientes.length; i < 6; i++) {
      const relleno = document.createElement("tr");
      relleno.className = "relleno";
      relleno.setAttribute("aria-hidden", "true");
      for (let c = 0; c < 3; c++) relleno.appendChild(document.createElement("td"));
      cuerpo.appendChild(relleno);
    }

    mensaje.className = "mensaje mensaje--centrado";
    mensaje.textContent = pendientes.length ? "" : "No hay fallas pendientes.";
  }

  // Lista de { idIncidencia, accion } con las fallas marcadas como Solucionado o Dar de baja
  function cambiosPendientes() {
    return Array.from(cuerpo.querySelectorAll(".selector-rol"))
      .filter((selector) => selector.value === SOLUCIONADO || selector.value === DAR_DE_BAJA)
      .map((selector) => ({ idIncidencia: Number(selector.dataset.idIncidencia), accion: selector.value }));
  }

  botonGuardar.addEventListener("click", async () => {
    const cambios = cambiosPendientes();

    if (cambios.length === 0) {
      mostrarAviso("Sin cambios", "No hay fallas marcadas como solucionadas o para dar de baja.");
      return;
    }

    const bajas = cambios.filter((c) => c.accion === DAR_DE_BAJA).length;
    if (bajas > 0 && !confirm(
      (bajas === 1 ? "1 bicicleta se dará" : bajas + " bicicletas se darán") +
      " de baja y no se podrán volver a usar. ¿Continuar?")) {
      return;
    }

    // Se procesan una por una: si alguna falla (p. ej. la bici está en un viaje), las demás siguen
    botonGuardar.disabled = true;
    let solucionadas = 0;
    let habilitadas = 0;
    let dadasDeBaja = 0;
    const errores = [];
    for (const cambio of cambios) {
      try {
        if (cambio.accion === DAR_DE_BAJA) {
          await repositorioIncidencias.darDeBaja(cambio.idIncidencia);
          dadasDeBaja++;
        } else {
          const resultado = await repositorioIncidencias.marcarAtendida(cambio.idIncidencia);
          solucionadas++;
          if (resultado.habilitada) habilitadas++;
        }
      } catch (error) {
        // p. ej. INCIDENCIA_YA_ATENDIDA si otra falla de la misma bici ya la dio de baja
        errores.push(error instanceof ErrorApi ? error.message : "Error inesperado.");
      }
    }
    botonGuardar.disabled = false;
    await pintarTabla(); // muestra lo que quedó guardado

    const partes = [];
    if (solucionadas) {
      partes.push(
        (solucionadas === 1 ? "Se solucionó 1 falla" : "Se solucionaron " + solucionadas + " fallas") +
        " (" + (habilitadas === 1 ? "1 bicicleta volvió" : habilitadas + " bicicletas volvieron") +
        " a estar disponible" + (habilitadas === 1 ? ")." : "s).")
      );
    }
    if (dadasDeBaja) {
      partes.push(dadasDeBaja === 1 ? "1 bicicleta fue dada de baja." : dadasDeBaja + " bicicletas fueron dadas de baja.");
    }
    if (errores.length) partes.push("No se pudo guardar: " + errores.join(" "));
    mostrarAviso(errores.length ? "Cambios guardados con avisos" : "Cambios guardados", partes.join(" "));
  });

  // Ícono de persona (arriba a la izquierda): vuelve al menú
  enlaceVolver.addEventListener("click", (evento) => {
    if (cambiosPendientes().length > 0 && !confirm("Tienes cambios sin guardar. ¿Quieres salir sin guardarlos?")) {
      evento.preventDefault();
    }
  });

  await pintarTabla();
});
