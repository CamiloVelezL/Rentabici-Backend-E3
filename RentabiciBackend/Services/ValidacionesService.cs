namespace RentabiciBackend.Services;

public class ValidacionesService
{
    /// <summary>
    /// Valida que el ID proporcionado sea mayor a cero.
    /// </summary>
    public bool EsIdValido(int id) => id > 0;
}