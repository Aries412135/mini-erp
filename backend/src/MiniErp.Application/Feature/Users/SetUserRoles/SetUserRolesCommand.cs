using MediatR;

namespace MiniErp.Application.Feature.Users.SetUserRoles;

public record SetUserRolesCommand(Guid UserId, IReadOnlyList<string> Roles) : IRequest;