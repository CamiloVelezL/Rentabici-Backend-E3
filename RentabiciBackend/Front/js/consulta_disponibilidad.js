/* RentaBici - Consulta Disponibilidad (los 3 roles, RU-02) */

document.addEventListener("DOMContentLoaded", async () => {
  const usuario = servicioSesion.exigirRol([ROLES.USUARIO, ROLES.TECNICO, ROLES.ADMINISTRADOR]);
  if (!usuario) return; // fue redirigido

  // El ícono de persona vuelve al menú que corresponde al rol
  document.getElementById("volver").href = servicioSesion.rutaMenu(usuario.idRol);

  const cuerpo = document.getElementById("cuerpo-tabla");
  let estaciones = [];
  let bicicletas = [];
  try {
    [estaciones, bicicletas] = await Promise.all([repositorioEstaciones.listar(), repositorioBicicletas.listar()]);
  } catch (error) {
    avisarError(error);
    return;
  }

  const clave = (modelo) => modelo.trim().toLowerCase();
  const modelos = new Map();
  bicicletas.forEach((bicicleta) => {
    if (!modelos.has(clave(bicicleta.modelo))) modelos.set(clave(bicicleta.modelo), bicicleta.modelo.trim());
  });

  function crearCelda(texto) {
    const celda = document.createElement("td");
    celda.textContent = texto;
    return celda;
  }

  modelos.forEach((nombreModelo, claveModelo) => {
    estaciones.forEach((estacion) => {
      const disponibles = bicicletas.filter(
        (bicicleta) =>
          clave(bicicleta.modelo) === claveModelo &&
          bicicleta.idEstacionActual === estacion.idEstacion &&
          bicicleta.estadoApi === "Disponible"
      ).length;

      const fila = document.createElement("tr");
      fila.appendChild(crearCelda(estacion.nombre));
      fila.appendChild(crearCelda(nombreModelo));
      fila.appendChild(crearCelda(disponibles));
      cuerpo.appendChild(fila);
    });
  });

  // Fila de relleno: prolonga el color de las columnas, como en el diseño
  const relleno = document.createElement("tr");
  relleno.className = "relleno";
  relleno.setAttribute("aria-hidden", "true");
  for (let i = 0; i < 3; i++) relleno.appendChild(document.createElement("td"));
  cuerpo.appendChild(relleno);
});
