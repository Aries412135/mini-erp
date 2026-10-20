using FluentValidation;

namespace MiniErp.Application.Feature.Users.SetUserRoles;

public class SetUserRolesCommandValidator: AbstractValidator<SetUserRolesCommand>
{
    public SetUserRolesCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();

        RuleFor(x => x.Roles)
            .NotEmpty()
            .WithMessage("User harus punya minimal satu role.");

        RuleForEach(x => x.Roles)
            .NotEmpty()
            .MaximumLength(50);
    }
}