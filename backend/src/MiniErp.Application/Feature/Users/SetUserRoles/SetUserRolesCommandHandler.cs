using FluentValidation;
using FluentValidation.Results;
using MediatR;
using MiniErp.Application.Common.Exceptions;
using MiniErp.Application.Common.Interface;
using MiniErp.Domain.Constants;
using MiniErp.Domain.Entities;

namespace MiniErp.Application.Feature.Users.SetUserRoles;

public class SetUserRolesCommandHandler : IRequestHandler<SetUserRolesCommand>
{
    private readonly IUserRepository _users;
    private readonly IRoleRepository _roles;
    private readonly ICurrentUser _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public SetUserRolesCommandHandler(
        IUserRepository users,
        IRoleRepository roles,
        ICurrentUser currentUser,
        IUnitOfWork unitOfWork)
    {
        _users = users;
        _roles = roles;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(SetUserRolesCommand request, CancellationToken cancellationToken)
    {
        var user = await _users.GetByIdForUpdateAsync(request.UserId, cancellationToken)
                    ?? throw new NotFoundException("User", request.UserId);
        
        var allRoles = await _roles.GetAllAsync(cancellationToken);

        var requestedNames = request.Roles
            .Select(r => r.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var requestedRoles = allRoles
            .Where(r => requestedNames.Contains(r.Name, StringComparer.OrdinalIgnoreCase))
            .ToList();

        var unknown = requestedNames
            .Except(requestedRoles.Select(r => r.Name), StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (unknown.Count > 0)
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(request.Roles), $"Role tidak dikenal: {string.Join(", ", unknown)}.")
            });
        }

        var adminRole = allRoles.Single(r => r.Name == RoleNames.Admin);
        var isAdminNow = user.UserRoles.Any(ur => ur.RoleId == adminRole.Id);
        var staysAdmin = requestedRoles.Any(r => r.Id == adminRole.Id);

        if (user.Id == _currentUser.UserId && isAdminNow && !staysAdmin)
        {
            throw new ConflictException("Tidak bisa mencabut role Admin dari akun sendiri.");
        }

        user.ReplaceRoles(requestedRoles.Select(r => r.Id));

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}