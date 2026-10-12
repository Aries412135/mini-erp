namespace MiniErp.Application.Common.Exceptions;

public class InvalidCredentialsExceptions : Exception
{
    public InvalidCredentialsExceptions()
        : base("Email atau password salah.")
    {
        
    }
}