/* RentaBici - Alquiler, paso 2: teléfono y tiempo → crea la reserva en el backend */

document.addEventListener("DOMContentLoaded", () => {
  const usuario = servicioSesion.exigirRol([ROLES.USUARIO, ROLES.TECNICO, ROLES.ADMINISTRADOR]);
  if (!usuario) return; // fue redirigido

  // Datos del paso anterior; sin ellos se vuelve a empezar
  let pendiente = null;
  try {
    pendiente = JSON.parse(sessionStorage.getItem("rentabici_alquiler_pendiente"));
  } catch (error) {
    pendiente = null;
  }
  if (!pendiente || !pendiente.modelo) {
    window.location.href = "alquiler.html";
    return;
  }

  const rutaMenu = servicioSesion.rutaMenu(usuario.idRol);
  const formulario = document.getElementById("formulario-datos");
  const selectorTiempo = document.getElementById("tiempo");
  const mensaje = document.getElementById("mensaje");
  const botonConfirmar = document.getElementById("confirmar");

  function mostrarMensaje(texto, tipo) {
    mensaje.textContent = texto;
    mensaje.className = "mensaje mensaje--" + tipo;
  }

  function nombreTiempo(minutos) {
    if (minutos < 60) return minutos + " minutos";
    const horas = Math.floor(minutos / 60);
    const resto = minutos % 60;
    return horas + (horas === 1 ? " hora" : " horas") + (resto ? " " + resto + " minutos" : "");
  }

  // Tiempos en lapsos de 30 minutos, hasta el límite permitido
  for (let minutos = TARIFAS.MINUTOS_LAPSO; minutos <= TARIFAS.LIMITE_MINUTOS; minutos += TARIFAS.MINUTOS_LAPSO) {
    const opcion = document.createElement("option");
    opcion.value = minutos;
    opcion.textContent = nombreTiempo(minutos);
    selectorTiempo.appendChild(opcion);
  }

  selectorTiempo.addEventListener("change", () => {
    mostrarMensaje("Costo estimado: " + formatearPesos(calcularCosto(Number(selectorTiempo.value))), "ok");
  });

  formulario.addEventListener("submit", async (evento) => {
    evento.preventDefault();
    mostrarMensaje("", "ok");

    const telefono = formulario.telefono.value.replace(/\s+/g, "");
    const minutos = Number(selectorTiempo.value);

    if (!/^\d{7,10}$/.test(telefono)) {
      mostrarMensaje("El teléfono debe tener entre 7 y 10 números.", "error");
      return;
    }
    if (!minutos) {
      mostrarMensaje("Elige el tiempo de uso.", "error");
      return;
    }

    botonConfirmar.disabled = true;
    let reserva;
    let estaciones;
    try {
      // El backend valida RN-01, RN-04, estación abierta y elige la bicicleta
      reserva = await repositorioReservas.agregar({
        idEstacion: pendiente.idEstacion,
        modelo: pendiente.modelo,
        fechaUso: pendiente.fecha,
        telefono,
        tiempoMinutos: minutos,
      });
      estaciones = await repositorioEstaciones.listar();
    } catch (error) {
      botonConfirmar.disabled = false;
      avisarError(error, mensaje); // SIN_BICICLETAS, LIMITE_ACTIVO, USUARIO_CON_SALDO...
      return;
    }
    sessionStorage.removeItem("rentabici_alquiler_pendiente");

    const estacion = estaciones.find((e) => e.idEstacion === reserva.idEstacion);
    mostrarAviso(
      "¡Reserva creada!",
      "Bicicleta " + String(reserva.idBicicleta).padStart(4, "0") + " (" + reserva.modelo + ") en " +
        (estacion ? estacion.nombre : "la estación") + " para el " + reserva.fechaUso + ", por " +
        nombreTiempo(reserva.tiempoMinutos) + ". Costo estimado: " + formatearPesos(reserva.costoEstimado) +
        ". Se libera si no la recibes en " + TARIFAS.MINUTOS_RESERVA +
        " minutos. El pago se hace en \"Pagos y multas\".",
      () => (window.location.href = rutaMenu)
    );
  });
});
