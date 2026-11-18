/* RentaBici - Control de bicicletas (Técnico y Administrador, RF-04) */

document.addEventListener("DOMContentLoaded", async () => {
  const usuario = servicioSesion.exigirRol([ROLES.TECNICO, ROLES.ADMINISTRADOR]);
  if (!usuario) return; // fue redirigido

  const cuerpo = document.getElementById("cuerpo-tabla");

  let estaciones = [];
  let bicicletas = [];
  try {
    [estaciones, bicicletas] = await Promise.all([repositorioEstaciones.listar(), repositorioBicicletas.listar()]);
  } catch (error) {
    avisarError(error);
    return;
  }

  // idEstacion -> nombre de la estación
  const nombresEstaciones = new Map(estaciones.map((estacion) => [estacion.idEstacion, estacion.nombre]));

  function crearCelda(texto) {
    const celda = document.createElement("td");
    celda.textContent = texto;
    return celda;
  }

  bicicletas.forEach((bicicleta) => {
    const fila = document.createElement("tr");
    fila.appendChild(crearCelda(bicicleta.modelo));
    fila.appendChild(crearCelda(nombresEstaciones.get(bicicleta.idEstacionActual) || (bicicleta.estadoApi === "Alquilada" ? "En viaje" : "Sin estación")));
    fila.appendChild(crearCelda(String(bicicleta.idBicicleta).padStart(4, "0")));
    fila.appendChild(crearCelda(bicicleta.estado));
    cuerpo.appendChild(fila);
  });

  // Fila de relleno: prolonga el color de las columnas, como en el diseño
  const relleno = document.createElement("tr");
  relleno.className = "relleno";
  relleno.setAttribute("aria-hidden", "true");
  for (let i = 0; i < 4; i++) relleno.appendChild(document.createElement("td"));
  cuerpo.appendChild(relleno);
});
