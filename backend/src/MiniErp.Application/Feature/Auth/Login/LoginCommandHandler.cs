// using System.Security.Authentication;
using MediatR;
using MiniErp.Application.Common.Interface;
using MiniErp.Application.Common.Exceptions;

namespace MiniErp.Application.Feature.Auth.Login;

public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthTokenResult>
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly RefreshTokenIssuer _refreshTokens;
    private readonly IUnitOfWork _unitOfWork;

    public LoginCommandHandler(
        IUserRepository users,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        RefreshTokenIssuer refreshToken,
        IUnitOfWork unitOfWork
    ) {
        _users = users;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _refreshTokens = refreshToken;
        _unitOfWork = unitOfWork;
    }

    public async Task<AuthTokenResult> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var user = await _users.GetByEmailAsync(email, cancellationToken);

        if (user is null
            || !user.IsActive
            || !_passwordHasher.Verify(user.PasswordHash, request.Password))
        {
            throw new InvalidCredentialsException();
        }

        var accesstoken = _jwtTokenGenerator.Generate(user);
        var refreshToken = _refreshTokens.Issue(user.Id, Guid.NewGuid());

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthTokenResult(
            accesstoken.Token, 
            accesstoken.ExpiresAtUtc,
            refreshToken.Token,
            refreshToken.ExpiresAtUtc);
    }
}