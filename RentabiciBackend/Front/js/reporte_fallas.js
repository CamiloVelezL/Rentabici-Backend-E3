/* RentaBici - Reporte de fallas (los 3 roles, RU-05 / RF-08) */

document.addEventListener("DOMContentLoaded", async () => {
  const usuario = servicioSesion.exigirRol([ROLES.USUARIO, ROLES.TECNICO, ROLES.ADMINISTRADOR]);
  if (!usuario) return; // fue redirigido

  const rutaMenu = servicioSesion.rutaMenu(usuario.idRol);
  document.getElementById("volver").href = rutaMenu;

  const formulario = document.getElementById("formulario-falla");
  const mensaje = document.getElementById("mensaje");
  const botonConfirmar = document.getElementById("confirmar");

  function mostrarMensaje(texto, tipo) {
    mensaje.textContent = texto;
    mensaje.className = "mensaje mensaje--" + tipo;
  }

  let estaciones = [];
  try {
    estaciones = await repositorioEstaciones.listar();
  } catch (error) {
    avisarError(error, mensaje);
    botonConfirmar.disabled = true;
    return;
  }

  const normalizar = (texto) =>
    texto.trim().toLowerCase().normalize("NFD").replace(/[̀-ͯ]/g, "").replace(/\s+/g, " ");

  function buscarEstacion(texto) {
    const buscado = normalizar(texto);
    return estaciones.find((e) => normalizar(e.nombre) === buscado || String(e.idEstacion) === buscado);
  }

  formulario.addEventListener("submit", async (evento) => {
    evento.preventDefault();
    mostrarMensaje("", "ok");

    const textoId = formulario["id-bicicleta"].value.trim();
    const textoEstacion = formulario.estacion.value;
    const gravedad = formulario.falla.value;

    if (!/^\d+$/.test(textoId)) {
      mostrarMensaje("Escribe el id de la bicicleta con números (por ejemplo 0001).", "error");
      return;
    }

    const estacion = buscarEstacion(textoEstacion);
    if (!estacion) {
      mostrarMensaje("No encontré esa estación. Escribe su nombre, por ejemplo Estación 1.", "error");
      return;
    }

    if (!GRAVEDADES_FALLA.includes(gravedad)) {
      mostrarMensaje("Elige la gravedad de la falla.", "error");
      return;
    }

    botonConfirmar.disabled = true;
    let resultado;
    try {
      // El backend aplica RN-05 y el patrón State (Revisión / Reparación)
      resultado = await repositorioIncidencias.crearReporte({
        idBicicleta: Number(textoId),
        idEstacion: estacion.idEstacion,
        gravedad,
      });
    } catch (error) {
      botonConfirmar.disabled = false;
      avisarError(error, mensaje); // BICICLETA_NO_EXISTE, BICICLETA_NO_ESTA_EN_ESTACION, REPORTE_DUPLICADO...
      return;
    }

    mostrarAviso("Falla reportada", resultado.mensaje, () => (window.location.href = rutaMenu));
  });
});
