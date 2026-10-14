// using System.Security.Authentication;
using Microsoft.Extensions.Time.Testing;
using MiniErp.Application.Common.Exceptions;
using MiniErp.Application.Common.Interface;
using MiniErp.Application.Feature.Auth;
using MiniErp.Application.Feature.Auth.Login;
using MiniErp.Domain.Entities;
using NSubstitute;

namespace MiniErp.Application.UnitTests.Features.Auth;

public class LoginCommandHandlerTests
{
    private static readonly DateTime Now = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
    private readonly IJwtTokenGenerator _jwt = Substitute.For<IJwtTokenGenerator>();
    private readonly IRefreshTokenGenerator _refreshGenerator = Substitute.For<IRefreshTokenGenerator>();
    private readonly IRefreshTokenRepository _refreshRepository = Substitute.For<IRefreshTokenRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly LoginCommandHandler _sut;

    public LoginCommandHandlerTests()
    {
        _refreshGenerator.Generate().Returns("raw-refresh");
        _refreshGenerator.Hash("raw-refresh").Returns("hash-refresh");
        _refreshGenerator.Lifetime.Returns(TimeSpan.FromDays(7));

        var issuer = new RefreshTokenIssuer(
            _refreshGenerator,
            _refreshRepository,
            new FakeTimeProvider(new DateTimeOffset(Now)));
        
        _sut = new LoginCommandHandler(_users, _hasher, _jwt, issuer, _unitOfWork);
    }

    [Fact]
    public async Task Handle_ValidCredentials_ReturnsAccessAndRefreshToken()
    {
        // Arrange
        var user = User.Create("a@b.com", "hash", "Admin Test");
        var accessExpires = DateTime.UtcNow.AddMinutes(15);

        _users.GetByEmailAsync("a@b.com", Arg.Any<CancellationToken>()).Returns(user);
        _hasher.Verify("hash", "secret123").Returns(true);
        _jwt.Generate(user).Returns(new AccessTokenResult("access-abc", accessExpires));

        // Act
        var result = await _sut.Handle(new LoginCommand("  A@B.com ", "secret123"), CancellationToken.None);

        // Assert
        Assert.Equal("access-abc", result.AccessToken);
        Assert.Equal(accessExpires, result.AccessTokenExpiresAtUtc);
        Assert.Equal("raw-refresh", result.RefreshToken);
        Assert.Equal(Now.AddDays(7), result.RefreshTokenExpiresAtUtc);
    }

    [Fact]
    public async Task Handle_ValidCredentials_StoresOnlyTheHasAndSaves()
    {
        var user = User.Create("a@b.com", "hash", "Admin Test");
        _users.GetByEmailAsync("a@b.com", Arg.Any<CancellationToken>()).Returns(user);
        _hasher.Verify("hash", "secret123").Returns(true);
        _jwt.Generate(user).Returns(new AccessTokenResult("access-abc", Now.AddMinutes(15)));

        await _sut.Handle(new LoginCommand("a@b.com", "secret123"), CancellationToken.None);

        _refreshRepository.Received(1).Add(Arg.Is<RefreshToken>(t =>
            t.UserId == user.Id && t.TokenHash == "hash-refresh"));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WrongPassword_ThrowsAndIssuesNothing()
    {
        var user = User.Create("a@b.com", "hash", "Admin Test");
        _users.GetByEmailAsync("a@b.com", Arg.Any<CancellationToken>()).Returns(user);
        _hasher.Verify("hash", "salah").Returns(false);

        await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => _sut.Handle(new LoginCommand("a@b.com", "salah"), CancellationToken.None));
        
        _refreshRepository.DidNotReceive().Add(Arg.Any<RefreshToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UnknownEmail_ThrowsInvalidCredentials()
    {
        _users.GetByEmailAsync("x@y.com", Arg.Any<CancellationToken>()).Returns((User?)null);

        await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => _sut.Handle(new LoginCommand("x@y.com", "apa-saja"), CancellationToken.None)
        );
    }

    [Fact]
    public async Task Handle_InactiveUser_ThrowsInvalidCredentials()
    {
        var user = User.Create("a@b.com", "hash", "Admin Test");
        user.Deactivate();

        _users.GetByEmailAsync("a@b.com", Arg.Any<CancellationToken>()).Returns(user);
        _hasher.Verify("hash", "secret123").Returns(true);

        await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => _sut.Handle(new LoginCommand("a@b.com", "secret123"), CancellationToken.None)
        );
    }
}