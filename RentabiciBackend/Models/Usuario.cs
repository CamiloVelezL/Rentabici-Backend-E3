namespace RentabiciBackend.Models;

/// <summary>
/// Representa a un cliente registrado dentro del sistema de alquiler Rentabici.
/// </summary>
public class Usuario
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public bool TieneMulta { get; set; }
    
    /// <summary>
    /// Correo electrónico de contacto y envío de notificaciones del usuario.
    /// </summary>
    public string Email { get; set; } = string.Empty;
}