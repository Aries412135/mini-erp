using FluentValidation;

namespace MiniErp.Application.Feature.Auth.Login;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256);
        
        RuleFor(x => x.Password)
            .NotEmpty()
            .MaximumLength(128);
    }
}