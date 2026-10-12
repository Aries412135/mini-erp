using MediatR;

namespace MiniErp.Application.Feature.Auth.Login;
public record LoginCommand(string Email, string Password) : IRequest<LoginResult>;