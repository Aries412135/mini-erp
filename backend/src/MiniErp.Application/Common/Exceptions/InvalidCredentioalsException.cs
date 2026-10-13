namespace MiniErp.Application.Common.Exceptions;

public class InvalidCredentialsException : Exception
{
    public InvalidCredentialsException()
        : base("Email atau password salah.")
    {
        
    }
}