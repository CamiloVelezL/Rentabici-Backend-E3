/* RentaBici - Agregar estación (solo Administrador, RF-03) */

document.addEventListener("DOMContentLoaded", () => {
  const usuario = servicioSesion.exigirRol(ROLES.ADMINISTRADOR);
  if (!usuario) return; // fue redirigido

  const formulario = document.getElementById("formulario-estacion");
  const mensaje = document.getElementById("mensaje");
  const botonConfirmar = document.getElementById("confirmar");

  function mostrarMensaje(texto, tipo) {
    mensaje.textContent = texto;
    mensaje.className = "mensaje mensaje--" + tipo;
  }

  formulario.addEventListener("submit", async (evento) => {
    evento.preventDefault();
    mostrarMensaje("", "ok");

    const nombre = formulario.nombre.value.trim();
    const ubicacion = formulario.ubicacion.value.trim();
    const capacidad = Number(formulario.cantidad.value);

    if (!Number.isInteger(capacidad) || capacidad < 1) {
      mostrarMensaje("La cantidad debe ser un número entero mayor que 0.", "error");
      return;
    }

    botonConfirmar.disabled = true;
    try {
      // La estación nueva queda abierta; el backend valida nombre repetido (ESTACION_DUPLICADA)
      await repositorioEstaciones.agregar({ nombre, direccion: ubicacion, capacidadMaxima: capacidad });
    } catch (error) {
      botonConfirmar.disabled = false;
      avisarError(error, mensaje);
      return;
    }

    mostrarMensaje("¡Estación agregada! Volviendo a Control de estaciones...", "ok");
    setTimeout(() => (window.location.href = "gestion_estaciones.html"), 1200);
  });
});
