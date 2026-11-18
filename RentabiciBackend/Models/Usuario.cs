using System.Text.Json.Serialization;

namespace RentabiciBackend.Models;

public class Usuario
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellidos { get; set; } = string.Empty;
    public string Documento { get; set; } = string.Empty;

    /// <summary>Correo o, para las cuentas de prueba, el nombre de usuario (sam, sam2, sam3).</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>RNF-02: nunca se guarda la contraseña en texto plano (PBKDF2 + sal).</summary>
    [JsonIgnore]
    public string PasswordHash { get; set; } = string.Empty;

    public Rol Rol { get; set; } = Rol.Usuario;

    /// <summary>Infracción sin saldar (RN-04).</summary>
    public bool TieneMulta { get; set; }

    /// <summary>Saldo de alquileres sin pagar (RN-04).</summary>
    public decimal SaldoPendiente { get; set; }

    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

    /// <summary>RN-04: bloqueado si tiene multa o saldo pendiente.</summary>
    public bool EstaBloqueado => TieneMulta || SaldoPendiente > 0;
}
