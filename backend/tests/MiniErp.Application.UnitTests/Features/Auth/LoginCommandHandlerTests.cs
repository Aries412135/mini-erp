using System.Security.Authentication;
using MiniErp.Application.Common.Interface;
using MiniErp.Application.Feature.Auth.Login;
using MiniErp.Domain.Entities;
using NSubstitute;

namespace MiniErp.Application.UnitTests.Features.Auth;

public class LoginCommandHandlerTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
    private readonly IJwtTokenGenerator _jwt = Substitute.For<IJwtTokenGenerator>();
    private readonly LoginCommandHandler _sut;

    public LoginCommandHandlerTests()
    {
        _sut = new LoginCommandHandler(_users, _hasher, _jwt);
    }

    [Fact]
    public async Task Handle_ValidCredentials_ReturnToken()
    {
        // Arrange
        var user = User.Create("a@b.com", "hash", "Admin Test");
        var expires = DateTime.UtcNow.AddMinutes(15);

        _users.GetByEmailAsync("a@b.com", Arg.Any<CancellationToken>()).Returns(user);
        _hasher.Verify("hash", "secret123").Returns(true);
        _jwt.Generate(user).Returns(new AccessTokenResult("token-abc", expires));

        // Act
        var result = await _sut.Handle(new LoginCommand("  A@B.com ", "secret123"), CancellationToken.None);

        // Assert
        Assert.Equal("token-abc", result.AccessToken);
        Assert.Equal(expires, result.ExpiresAtUtc);
    }

    [Fact]
    public async Task Handle_WrongPassword_ThrowsInvalidCredentials()
    {
        var user = User.Create("a@b.com", "hash", "Admin Test");
        _users.GetByEmailAsync("a@b.com", Arg.Any<CancellationToken>()).Returns(user);
        _hasher.Verify("hash", "salah").Returns(false);

        await Assert.ThrowsAsync<InvalidCredentialException>(
            () => _sut.Handle(new LoginCommand("a@b.com", "salah"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_UnknownEmail_ThrowsInvalidCredentials()
    {
        _users.GetByEmailAsync("x@y.com", Arg.Any<CancellationToken>()).Returns((User?)null);

        await Assert.ThrowsAsync<InvalidCredentialException>(
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

        await Assert.ThrowsAsync<InvalidCredentialException>(
            () => _sut.Handle(new LoginCommand("a@b.com", "secret123"), CancellationToken.None)
        );
    }
}