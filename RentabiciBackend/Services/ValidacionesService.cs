namespace RentabiciBackend.Services;
public class ValidacionesService
{
    public bool EsIdValido(int id) => id > 0;
}