namespace MiniErp.Application.Feature.Auth.Login;

public record AuthTokenResult(
    string AccessToken, 
    DateTime AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTime RefreshTokenExpiresAtUtc);