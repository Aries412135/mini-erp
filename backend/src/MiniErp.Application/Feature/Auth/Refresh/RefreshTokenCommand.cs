using MediatR;
using MiniErp.Application.Feature.Auth.Login;

namespace MiniErp.Application.Feature.Auth.Refresh;

public record RefreshTokenCommand(string RefreshToken) : IRequest<AuthTokenResult>;