/* RentaBici - Recibir Bicicleta (los 3 roles, RF-06) */

document.addEventListener("DOMContentLoaded", async () => {
  const usuario = servicioSesion.exigirRol([ROLES.USUARIO, ROLES.TECNICO, ROLES.ADMINISTRADOR]);
  if (!usuario) return; // fue redirigido

  // Solo tiene sentido con una reserva que todavía espera en la estación
  let reserva = null;
  try {
    reserva = await repositorioReservas.buscarActivaDeUsuario();
  } catch (error) {
    avisarError(error);
    return;
  }
  if (!reserva) {
    window.location.href = "reservas.html";
    return;
  }

  const boton = document.getElementById("recibida");
  boton.addEventListener("click", async () => {
    boton.disabled = true;
    try {
      // Pasa por RentalServiceProxy (multas, saldo, RN-01) y luego por el patrón State
      await repositorioAlquileres.iniciarViaje(reserva);
      window.location.href = servicioSesion.rutaMenu(usuario.idRol);
    } catch (error) {
      boton.disabled = false;
      avisarError(error); // p. ej. la reserva expiró (RN-02) o la bici fue reportada
    }
  });
});
