/* RentaBici - Pagos y multas (los 3 roles, RU-08 / RF-10 / RF-11) */

document.addEventListener("DOMContentLoaded", async () => {
  const usuario = servicioSesion.exigirRol([ROLES.USUARIO, ROLES.TECNICO, ROLES.ADMINISTRADOR]);
  if (!usuario) return; // fue redirigido

  document.getElementById("volver").href = servicioSesion.rutaMenu(usuario.idRol);

  const cuerpo = document.getElementById("cuerpo-tabla");
  const mensaje = document.getElementById("mensaje");
  const factura = document.getElementById("factura");
  const botonPagar = document.getElementById("pagar");

  function crearFila(tipo, cantidad) {
    const fila = document.createElement("tr");
    [tipo, cantidad].forEach((texto) => {
      const celda = document.createElement("td");
      celda.textContent = texto;
      fila.appendChild(celda);
    });
    return fila;
  }

  function desactivarPago() {
    botonPagar.classList.add("boton--inactivo");
    botonPagar.setAttribute("aria-disabled", "true");
    botonPagar.addEventListener("click", (evento) => evento.preventDefault());
  }

  // Una fila "Alquiler" por viaje y una "Multa" cuando hubo demora o infracción (calculadas en el backend),
  // agrupadas en una factura con código
  let pendientes = { items: [], total: 0 };
  try {
    pendientes = await repositorioPagos.pendientes();
  } catch (error) {
    avisarError(error, mensaje);
    desactivarPago();
    return;
  }

  pendientes.items.forEach((item) => cuerpo.appendChild(crearFila(item.tipo, formatearPesos(item.monto))));

  for (let i = pendientes.items.length; i < 5; i++) {
    const relleno = document.createElement("tr");
    relleno.className = "relleno";
    relleno.setAttribute("aria-hidden", "true");
    relleno.appendChild(document.createElement("td"));
    relleno.appendChild(document.createElement("td"));
    cuerpo.appendChild(relleno);
  }

  if (!pendientes.items.length) {
    mensaje.textContent = "No tienes pagos pendientes.";
    desactivarPago();
  } else {
    mensaje.textContent = "Total a pagar: " + formatearPesos(pendientes.total);
    // El usuario debe escribir este código en la pantalla de pago
    factura.textContent = "Código de factura: " + pendientes.codigoFactura;
    factura.style.fontWeight = "500";
  }
});
