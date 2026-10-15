using System.ComponentModel;
using MediatR;
using MiniErp.Application.Common.Interface;

namespace MiniErp.Application.Feature.Auth.Logout;

public class LogoutCommandHandler : IRequestHandler<LogoutCommand>
{
    private readonly IRefreshTokenRepository _tokens;
    private readonly IRefreshTokenGenerator _generator;
    private readonly TimeProvider _time;

    public LogoutCommandHandler(
        IRefreshTokenRepository tokens,
        IRefreshTokenGenerator generator,
        TimeProvider time)
    {
        _tokens = tokens;
        _generator = generator;
        _time = time;
    }

    public async Task Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = _generator.Hash(request.RefreshToken);
        var stored = await _tokens.GetByHashAsync(tokenHash, cancellationToken);

        if (stored is null)
        {
            return; // idempotent: tidak ada yang perlu dicabut, dan tidak ada info yang bocor
        }

        var now = _time.GetUtcNow().UtcDateTime;
        await _tokens.RevokeFamilyAsync(stored.FamilyId, now, cancellationToken);
    }
}