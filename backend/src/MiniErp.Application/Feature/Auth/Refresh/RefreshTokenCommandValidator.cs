using FluentValidation;

namespace MiniErp.Application.Feature.Auth.Refresh;

public class RefresTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefresTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty()
            .MaximumLength(200);
    }
}