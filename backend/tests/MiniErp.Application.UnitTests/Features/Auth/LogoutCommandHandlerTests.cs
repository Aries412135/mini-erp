using System.ComponentModel;
using System.Net;
using Microsoft.Extensions.Time.Testing;
using MiniErp.Application.Common.Interface;
using MiniErp.Application.Feature.Auth.Logout;
using MiniErp.Domain.Entities;
using NSubstitute;

namespace MiniErp.Application.UnitTests.Features.Auth;

public class LogoutCommandHandlerTests
{
    private static readonly DateTime Now = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
    private const string RawToken = "raw-token";
    private const string TokenHash = "hash-token";

    private readonly IRefreshTokenRepository _tokens = Substitute.For<IRefreshTokenRepository>();
    private readonly IRefreshTokenGenerator _generator = Substitute.For<IRefreshTokenGenerator>();
    private readonly LogoutCommandHandler _sut;

    public LogoutCommandHandlerTests()
    {
        _generator.Hash(RawToken).Returns(TokenHash);

        _sut = new LogoutCommandHandler(
            _tokens,
            _generator,
            new FakeTimeProvider(new DateTimeOffset(Now)));
    }

    private static RefreshToken StoredToken() =>
        RefreshToken.Create(Guid.NewGuid(), Guid.NewGuid(), TokenHash, Now.AddHours(-1), TimeSpan.FromDays(7));
    

    [Fact]
    public async Task Handle_KnownToken_RevokesItsWholeFamily()
    {
        var stored = StoredToken();
        _tokens.GetByHashAsync(TokenHash, Arg.Any<CancellationToken>()).Returns(stored);

        await _sut.Handle(new LogoutCommand(RawToken), CancellationToken.None);

        await _tokens.Received(1).RevokeFamilyAsync(stored.FamilyId, Now, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UnknownToken_DoesNothingAndDoesNotThrow()
    {
        _tokens.GetByHashAsync(TokenHash, Arg.Any<CancellationToken>()).Returns((RefreshToken?)null);

        await _sut.Handle(new LogoutCommand(RawToken), CancellationToken.None);

        await _tokens.DidNotReceive().RevokeFamilyAsync(
            Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AlreadyUsedToken_StillRevokesFamily()
    {
        var stored = StoredToken();
        stored.MarkUsed(Now.AddMinutes(-30));
        _tokens.GetByHashAsync(TokenHash, Arg.Any<CancellationToken>()).Returns(stored);

        await _sut.Handle(new LogoutCommand(RawToken), CancellationToken.None);
        await _tokens.Received(1).RevokeFamilyAsync(stored.FamilyId, Now, Arg.Any<CancellationToken>());
    }
}