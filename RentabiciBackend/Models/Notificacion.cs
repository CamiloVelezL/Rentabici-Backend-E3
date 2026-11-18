namespace RentabiciBackend.Models;

public class Notificacion
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public CanalNotificacion Canal { get; set; }
    public string Asunto { get; set; } = string.Empty;
    public string Mensaje { get; set; } = string.Empty;
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
}
