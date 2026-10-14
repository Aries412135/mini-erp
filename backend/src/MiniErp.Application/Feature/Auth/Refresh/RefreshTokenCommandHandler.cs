using MediatR;
using MiniErp.Application.Common.Exceptions;
using MiniErp.Application.Common.Interface;
using MiniErp.Application.Feature.Auth.Login;
using MiniErp.Domain.Entities;

namespace MiniErp.Application.Feature.Auth.Refresh;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthTokenResult>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IRefreshTokenGenerator _generator;
    private readonly IUserRepository _users;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly RefreshTokenIssuer _issuer;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _time;

    public RefreshTokenCommandHandler(
        IRefreshTokenRepository refreshTokenRepository,
        IRefreshTokenGenerator generator,
        IUserRepository users,
        IJwtTokenGenerator jwtTokenGenerator,
        RefreshTokenIssuer issuer,
        IUnitOfWork unitOfWork,
        TimeProvider time)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _generator = generator;
        _users = users;
        _jwtTokenGenerator = jwtTokenGenerator;
        _issuer = issuer;
        _unitOfWork = unitOfWork;
        _time = time;
    }

    public async Task<AuthTokenResult> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var now = _time.GetUtcNow().UtcDateTime;
        var tokenHash = _generator.Hash(request.RefreshToken);
        
        var stored = await _refreshTokenRepository.GetByHashAsync(tokenHash, cancellationToken);

        if (stored is null)
        {
            throw new InvalidRefreshTokenException();
        }

        // Token yang sudah terpakai / dicabut muncul lagi: ada salinan yang bocor
        if (stored.IsUsed || stored.IsRevoked)
        {
            await _refreshTokenRepository.RevokeFamilyAsync(stored.FamilyId, now, cancellationToken);
            throw new InvalidRefreshTokenException();
        }

        if (stored.IsExpired(now))
        {
            throw new InvalidRefreshTokenException();
        }

        // Klaim atomik: hanya SATU request yang boleh "memakai" token ini.
        var claimed = await _refreshTokenRepository.TryMarkUsedAsync(stored.Id, now,cancellationToken);

        if (!claimed)
        {
            await _refreshTokenRepository.RevokeFamilyAsync(stored.FamilyId, now, cancellationToken);
            throw new InvalidRefreshTokenException();
        }

        var user = await _users.GetByIdAsync(stored.UserId, cancellationToken);

        if (user is null || !user.IsActive)
        {
            await _refreshTokenRepository.RevokeFamilyAsync(stored.FamilyId, now, cancellationToken);
            throw new InvalidRefreshTokenException();
        }

        var accessToken = _jwtTokenGenerator.Generate(user);
        var newRefreshToken = _issuer.Issue(user.Id, stored.FamilyId);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthTokenResult(
            accessToken.Token,
            accessToken.ExpiresAtUtc,
            newRefreshToken.Token,
            newRefreshToken.ExpiresAtUtc);
    }
}