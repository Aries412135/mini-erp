namespace MiniErp.Application.Feature.Auth.Login;

public record LoginResult(string AccessToken, DateTime ExpiresAtUtc);