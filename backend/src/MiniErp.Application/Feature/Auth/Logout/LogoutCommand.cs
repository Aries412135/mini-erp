using MediatR;

namespace MiniErp.Application.Feature.Auth.Logout;

public record LogoutCommand(string RefreshToken) : IRequest;