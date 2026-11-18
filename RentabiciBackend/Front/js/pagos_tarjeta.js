/* RentaBici - Pago con código de factura (los 3 roles, RF-10)
 * El usuario escribe el código que aparece en "Pagos y multas" (p. ej. RB-0001).
 * El backend valida que la factura exista, sea suya y no esté pagada; luego confirma
 * el pago (PaymentFacade), desbloquea al usuario (RN-04) y envía el comprobante (RF-12). */

document.addEventListener("DOMContentLoaded", async () => {
  const usuario = servicioSesion.exigirRol([ROLES.USUARIO, ROLES.TECNICO, ROLES.ADMINISTRADOR]);
  if (!usuario) return; // fue redirigido

  const formulario = document.getElementById("formulario-pago");
  const campoCodigo = document.getElementById("codigo-factura");
  const mensaje = document.getElementById("mensaje");
  const botonConfirmar = document.getElementById("confirmar");

  function mostrarMensaje(texto, tipo) {
    mensaje.textContent = texto;
    mensaje.className = "mensaje mensaje--" + tipo;
  }

  // Sin nada que pagar, se vuelve a Pagos y multas
  try {
    const pendientes = await repositorioPagos.pendientes();
    if (!pendientes.items.length) {
      window.location.href = "pagos.html";
      return;
    }
  } catch (error) {
    avisarError(error, mensaje);
    botonConfirmar.disabled = true;
    return;
  }

  formulario.addEventListener("submit", async (evento) => {
    evento.preventDefault();
    mostrarMensaje("", "ok");

    const codigo = campoCodigo.value.trim().toUpperCase();

    // Validación rápida en el navegador; el backend repite todas las validaciones
    if (!codigo) {
      mostrarMensaje("Escribe el código de la factura.", "error");
      return;
    }
    if (!/^RB-\d{4,6}$/.test(codigo)) {
      mostrarMensaje("El código debe tener el formato RB-0000.", "error");
      return;
    }

    botonConfirmar.disabled = true;
    let resultado;
    try {
      resultado = await repositorioPagos.pagarFactura(codigo);
    } catch (error) {
      botonConfirmar.disabled = false;
      avisarError(error, mensaje); // FACTURA_NO_EXISTE, FACTURA_YA_PAGADA, factura de otro usuario...
      return;
    }

    mostrarAviso(
      "¡Pago realizado!",
      "Se pagó la factura " + resultado.codigoFactura + " por " + formatearPesos(resultado.totalPagado) +
        ". Comprobante: " + resultado.comprobante + ". Gracias por usar RentaBici.",
      () => (window.location.href = servicioSesion.rutaMenu(usuario.idRol))
    );
  });
});
