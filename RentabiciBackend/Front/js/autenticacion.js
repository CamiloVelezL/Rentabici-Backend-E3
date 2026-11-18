/* RentaBici - Autenticación (registro e inicio de sesión contra la API).
 * La contraseña ya no se hashea en el navegador: se envía al backend, que la
 * guarda con PBKDF2 + sal (RNF-02) y valida documento y correo duplicados (RF-01). */

class ServicioAutenticacion {
  constructor(api) {
    this.api = api;
  }

  // Registro público (formulario): el backend asigna SIEMPRE el rol "Usuario".
  async registrar({ nombre, apellidos, email, documento, contrasena }) {
    const r = await this.api.post("/auth/registro", {
      nombre: nombre.trim(),
      apellidos: apellidos.trim(),
      email: email.trim().toLowerCase(),
      documento: documento.trim(),
      password: contrasena,
    });
    return convertir.usuario(r.datos);
  }

  // Inicio de sesión: acepta el correo o el usuario (p. ej. "sam").
  // Devuelve { usuario, token }.
  async iniciarSesion(identificador, contrasena) {
    const r = await this.api.post("/auth/login", { usuario: identificador.trim(), password: contrasena });
    return { usuario: convertir.usuario(r.datos.usuario), token: r.datos.token };
  }
}

// Se mantiene asíncrona para no cambiar login.js / registro.js
async function crearServicioAutenticacion() {
  return new ServicioAutenticacion(clienteApi);
}
