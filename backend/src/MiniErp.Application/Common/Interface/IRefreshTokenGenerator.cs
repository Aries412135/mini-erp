namespace MiniErp.Application.Common.Interface;

public interface IRefreshTokenGenerator
{
    TimeSpan Lifetime { get; }
    string Generate();
    string Hash(string token);
}