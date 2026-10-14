using MiniErp.Application.Common.Interface;
using MiniErp.Domain.Entities;

namespace MiniErp.Application.Feature.Auth;

public record IssuedRefreshToken(string Token, DateTime ExpiresAtUtc);

public class RefreshTokenIssuer
{
    private readonly IRefreshTokenGenerator _generator;
    private readonly IRefreshTokenRepository _repository;
    private readonly TimeProvider _time;

    public RefreshTokenIssuer(
        IRefreshTokenGenerator generator,
        IRefreshTokenRepository repository,
        TimeProvider time)
    {
        _generator = generator;
        _repository = repository;
        _time = time;
    }

    public IssuedRefreshToken Issue(Guid userId, Guid familyId)
    {
        var rawToken = _generator.Generate();
        var now = _time.GetUtcNow().UtcDateTime;

        var entity = RefreshToken.Create(
            userId,
            familyId,
            _generator.Hash(rawToken),
            now,
            _generator.Lifetime);
        
        _repository.Add(entity);

        return new IssuedRefreshToken(rawToken, entity.ExpiresAt);
    }
}