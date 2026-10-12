using MiniErp.Domain.Entities;

namespace MiniErp.Application.Common.Interface;

public record AccessTokenResult(string Token, DateTime ExpiresAtUtc);
public interface IJwtTokenGenerator
{
    AccessTokenResult Generate(User user);
}