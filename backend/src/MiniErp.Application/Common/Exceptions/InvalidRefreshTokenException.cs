namespace MiniErp.Application.Common.Exceptions;

public class InvalidRefreshTokenException : Exception
{
    public InvalidRefreshTokenException()
        : base("Refresh token tidak valid atau sudah kadaluwarsa.")
    {
        
    }
}