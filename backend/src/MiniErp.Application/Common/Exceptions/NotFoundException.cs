public class NotFoundException : Exception
{
    public NotFoundException(string entity, object key)
        : base($"{entity} dengan id '{key}' tidak ditemukan.")
    {        
    }
}