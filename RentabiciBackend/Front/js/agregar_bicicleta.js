/* RentaBici - Agregar bicicleta (Técnico y Administrador, RF-04) */

document.addEventListener("DOMContentLoaded", async () => {
  const usuario = servicioSesion.exigirRol([ROLES.TECNICO, ROLES.ADMINISTRADOR]);
  if (!usuario) return; // fue redirigido

  const formulario = document.getElementById("formulario-bicicleta");
  const selectorEstacion = document.getElementById("estacion");
  const mensaje = document.getElementById("mensaje");
  const botonConfirmar = document.getElementById("confirmar");

  function mostrarMensaje(texto, tipo) {
    mensaje.textContent = texto;
    mensaje.className = "mensaje mensaje--" + tipo;
  }

  // Lista de estaciones registradas para elegir la ubicación
  try {
    (await repositorioEstaciones.listar()).forEach((estacion) => {
      const opcion = document.createElement("option");
      opcion.value = estacion.idEstacion;
      opcion.textContent = estacion.nombre;
      selectorEstacion.appendChild(opcion);
    });
  } catch (error) {
    avisarError(error, mensaje);
    botonConfirmar.disabled = true;
    return;
  }

  formulario.addEventListener("submit", async (evento) => {
    evento.preventDefault();
    mostrarMensaje("", "ok");

    const modelo = formulario.modelo.value.trim();
    const idEstacion = Number(selectorEstacion.value);

    if (!modelo || !idEstacion) {
      mostrarMensaje("Escribe el modelo y elige una estación.", "error");
      return;
    }

    botonConfirmar.disabled = true;
    let bicicleta;
    try {
      // El backend valida RN-03: no caben más bicicletas que la capacidad de la estación
      bicicleta = await repositorioBicicletas.agregar({ modelo, idEstacionActual: idEstacion });
    } catch (error) {
      botonConfirmar.disabled = false;
      avisarError(error, mensaje); // ESTACION_LLENA, VALIDACION
      return;
    }

    mostrarMensaje(
      "¡Bicicleta " + String(bicicleta.idBicicleta).padStart(4, "0") + " agregada! Volviendo a Control de bicicletas...",
      "ok"
    );
    setTimeout(() => (window.location.href = "gestion_bicicletas.html"), 1200);
  });
});
