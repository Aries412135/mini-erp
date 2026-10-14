using System.ComponentModel;
using Microsoft.Extensions.Time.Testing;
using Microsoft.VisualBasic;
using MiniErp.Application.Common.Exceptions;
using MiniErp.Application.Common.Interface;
using MiniErp.Application.Feature.Auth;
using MiniErp.Application.Feature.Auth.Refresh;
using MiniErp.Domain.Entities;
using NSubstitute;

namespace MiniErp.Application.UnitTests.Features.Auth;

public class RefreshTokenCommandHandlerTest
{
    private static readonly DateTime Now = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
    private static readonly TimeSpan Lifetime = TimeSpan.FromDays(7);

    private readonly IRefreshTokenRepository _tokens = Substitute.For<IRefreshTokenRepository>();
    private readonly IRefreshTokenGenerator _generator = Substitute.For<IRefreshTokenGenerator>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IJwtTokenGenerator _jwt = Substitute.For<IJwtTokenGenerator>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly RefreshTokenCommandHandler _sut;

    private readonly User _user = User.Create("a@a", "hash", "Admin Test");

    public RefreshTokenCommandHandlerTest()
    {
        _generator.Hash("raw-old").Returns("hash-old");
        _generator.Generate().Returns("raw-new");
        _generator.Hash("raw-new").Returns("hash-new");
        _generator.Lifetime.Returns(Lifetime);

        var time = new FakeTimeProvider(new DateTimeOffset(Now));
        var issuer = new RefreshTokenIssuer(_generator, _tokens, time);

        _sut = new RefreshTokenCommandHandler(_tokens, _generator, _users, _jwt, issuer, _unitOfWork, time);
    }

    private RefreshToken StoredToken() =>
        RefreshToken.Create(_user.Id, Guid.NewGuid(), "hash-old", Now.AddHours(-1), Lifetime);
    

    [Fact]
    public async Task Handle_ValidToken_RotatesAndReturnsNewTokens()
    {
        var stored = StoredToken();
        _tokens.GetByHashAsync("hash-old", Arg.Any<CancellationToken>()).Returns(stored);
        _tokens.TryMarkUsedAsync(stored.Id, Now, Arg.Any<CancellationToken>()).Returns(true);
        _users.GetByIdAsync(_user.Id, Arg.Any<CancellationToken>()).Returns(_user);
        _jwt.Generate(_user).Returns(new AccessTokenResult("access-new", Now.AddMinutes(15)));

        var result = await _sut.Handle(new RefreshTokenCommand("raw-old"), CancellationToken.None);

        Assert.Equal("access-new", result.AccessToken);
        Assert.Equal("raw-new", result.RefreshToken);
        // _tokens.Received(1).Add(Arg.Is<RefreshToken>( t =>
        //     t.FamilyId == stored.FamilyId && t.TokenHash == "hash-new"));
        var added = (RefreshToken)_tokens.ReceivedCalls()
            .Single(c => c.GetMethodInfo().Name == nameof(IRefreshTokenRepository.Add))
            .GetArguments()[0];
        Assert.Equal(stored.FamilyId, added.FamilyId);
        Assert.Equal("hash-new", added.TokenHash);

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _tokens.DidNotReceive().RevokeFamilyAsync(
            Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UnknownToken_Throws()
    {
        _tokens.GetByHashAsync("hash-old", Arg.Any<CancellationToken>()).Returns ((RefreshToken?)null);

        await Assert.ThrowsAsync<InvalidRefreshTokenException>(
            () => _sut.Handle(new RefreshTokenCommand("raw-hold"), CancellationToken.None));
        
        await _tokens.DidNotReceive().TryMarkUsedAsync(
            Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AlreadyUsedToken_RevokesWholeFamilyAndThrows()
    {
        var stored = StoredToken();
        stored.MarkUsed(Now.AddMinutes(-30));
        _tokens.GetByHashAsync("hash-old", Arg.Any<CancellationToken>()).Returns(stored);

        await Assert.ThrowsAsync<InvalidRefreshTokenException>(
            () => _sut.Handle(new RefreshTokenCommand("raw-old"), CancellationToken.None));
        
        await _tokens.Received(1).RevokeFamilyAsync(stored.FamilyId, Now, Arg.Any<CancellationToken>());
        _tokens.DidNotReceive().Add(Arg.Any<RefreshToken>());
    }

    [Fact]
    public async Task Handle_ExpiredToken_ThrowsWithoutRevokingFamily()
    {
        var stored = RefreshToken.Create(_user.Id, Guid.NewGuid(), "hash-old", Now.AddDays(-8), Lifetime);
        _tokens.GetByHashAsync("hash-old", Arg.Any<CancellationToken>()).Returns(stored);

        await Assert.ThrowsAsync<InvalidRefreshTokenException>(
            () => _sut.Handle(new RefreshTokenCommand("raw-old"), CancellationToken.None));
    
        await _tokens.DidNotReceive().RevokeFamilyAsync(
            Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_LostTheRace_TreatedAsReuseAndRevokesFamily()
    {
        var stored = StoredToken();
        _tokens.GetByHashAsync("hash-old", Arg.Any<CancellationToken>()).Returns(stored);
        _tokens.TryMarkUsedAsync(stored.Id, Now, Arg.Any<CancellationToken>()).Returns(false);

        await Assert.ThrowsAsync<InvalidRefreshTokenException>(
            () => _sut.Handle(new RefreshTokenCommand("raw-old"), CancellationToken.None));
        
        await _tokens.Received(1).RevokeFamilyAsync(stored.FamilyId, Now, Arg.Any<CancellationToken>());
        _tokens.DidNotReceive().Add(Arg.Any<RefreshToken>());
    }

    [Fact]
    public async Task Handle_InactiveUser_RevokesFamilyAndThrows()
    {
        var stored = StoredToken();
        _user.Deactivate();
        _tokens.GetByHashAsync("hash-old", Arg.Any<CancellationToken>()).Returns(stored);
        _tokens.TryMarkUsedAsync(stored.Id, Now, Arg.Any<CancellationToken>()).Returns(true);
        _users.GetByIdAsync(_user.Id, Arg.Any<CancellationToken>()).Returns(_user);

        await Assert.ThrowsAsync<InvalidRefreshTokenException>(
            () => _sut.Handle(new RefreshTokenCommand("raw-old"), CancellationToken.None));

        await _tokens.Received(1).RevokeFamilyAsync(stored.FamilyId, Now, Arg.Any<CancellationToken>());
        _tokens.DidNotReceive().Add(Arg.Any<RefreshToken>());
    }
}