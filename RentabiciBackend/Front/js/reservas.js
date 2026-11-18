/* RentaBici - Administrar Reserva (los 3 roles, RU-03 / RU-04) */

document.addEventListener("DOMContentLoaded", async () => {
  const usuario = servicioSesion.exigirRol([ROLES.USUARIO, ROLES.TECNICO, ROLES.ADMINISTRADOR]);
  if (!usuario) return; // fue redirigido

  const rutaMenu = servicioSesion.rutaMenu(usuario.idRol);
  document.getElementById("volver").href = rutaMenu;

  const campoId = document.getElementById("id-bicicleta");
  const campoEstado = document.getElementById("estado");
  const campoTiempo = document.getElementById("tiempo");
  const mensaje = document.getElementById("mensaje");
  const botones = {
    recibir: document.getElementById("recibir"),
    entregar: document.getElementById("entregar"),
    cancelar: document.getElementById("cancelar"),
  };

  // Lo que se muestra en pantalla (se recarga desde el backend en cada acción)
  let alquiler = null;
  let reserva = null;

  function nombreTiempo(minutos) {
    if (!minutos) return "Libre";
    if (minutos < 60) return minutos + " minutos";
    const horas = Math.floor(minutos / 60);
    const resto = minutos % 60;
    return horas + (horas === 1 ? " hora" : " horas") + (resto ? " " + resto + " minutos" : "");
  }

  async function pintar() {
    mensaje.textContent = "";
    mensaje.className = "mensaje";
    try {
      alquiler = await repositorioAlquileres.buscarEnCursoDeUsuario();
      reserva = alquiler ? null : await repositorioReservas.buscarActivaDeUsuario();
    } catch (error) {
      Object.values(botones).forEach((boton) => (boton.disabled = true));
      avisarError(error, mensaje);
      return;
    }

    if (!alquiler && !reserva) {
      campoId.value = "";
      campoEstado.value = "";
      campoTiempo.value = "";
      Object.values(botones).forEach((boton) => (boton.disabled = true));
      mensaje.textContent = "No tienes una reserva vigente.";
      return;
    }

    const enUso = Boolean(alquiler);
    const vigente = alquiler || reserva;
    campoId.value = String(vigente.idBicicleta).padStart(4, "0");
    campoEstado.value = enUso ? "En uso" : "En estación";
    campoTiempo.value = nombreTiempo(vigente.tiempoMinutos);
    botones.recibir.disabled = enUso;   // ya se recibió
    botones.entregar.disabled = !enUso;
    botones.cancelar.disabled = enUso;
  }

  botones.recibir.addEventListener("click", () => {
    if (!reserva) return pintar();
    window.location.href = "recibir_qr.html";
  });

  botones.entregar.addEventListener("click", async () => {
    if (!alquiler) return pintar();
    botones.entregar.disabled = true;
    let resultado;
    try {
      // El backend valida RN-03 (estación llena / cerrada), calcula la tarifa (Strategy)
      // y deja el pago pendiente (RN-04)
      resultado = await repositorioAlquileres.finalizarViaje(alquiler.idAlquiler);
    } catch (error) {
      botones.entregar.disabled = false;
      avisarError(error, mensaje);
      return;
    }
    await pintar();
    const multa = resultado.montoMulta > 0 ? " (incluye " + formatearPesos(resultado.montoMulta) + " por demora)" : "";
    mostrarAviso(
      "Bicicleta entregada",
      "Gracias por usar RentaBici. Costo del viaje: " + formatearPesos(resultado.tarifaTotal) + multa +
        ". Puedes pagarlo en \"Pagos y multas\".",
      () => (window.location.href = rutaMenu)
    );
  });

  botones.cancelar.addEventListener("click", () => {
    if (!reserva) return pintar();
    const actual = reserva;

    mostrarConfirmacion(
      "Cancelar reserva",
      "¿Seguro que quieres cancelar la reserva de la bicicleta " + String(actual.idBicicleta).padStart(4, "0") + "?",
      "Sí, cancelar",
      "No",
      async () => {
        try {
          await repositorioReservas.cancelar(actual.idReserva);
        } catch (error) {
          avisarError(error, mensaje);
          return;
        }
        await pintar();
        mensaje.textContent = "Reserva cancelada.";
        mensaje.className = "mensaje mensaje--ok";
      }
    );
  });

  await pintar();
});
