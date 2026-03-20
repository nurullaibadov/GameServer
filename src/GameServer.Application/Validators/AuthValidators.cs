using FluentValidation;
using GameServer.Application.DTOs.Auth;

namespace GameServer.Application.Validators;

public class RegisterRequestValidator : AbstractValidator<RegisterRequestDto>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Username).NotEmpty().MinimumLength(3).MaximumLength(30)
            .Matches("^[a-zA-Z0-9_]+$").WithMessage("Only letters, numbers, underscores.");
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(255);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8)
            .Matches("[A-Z]").WithMessage("Need uppercase.")
            .Matches("[a-z]").WithMessage("Need lowercase.")
            .Matches("[0-9]").WithMessage("Need number.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Need special char.");
        RuleFor(x => x.ConfirmPassword).Equal(x => x.Password).WithMessage("Passwords must match.");
        RuleFor(x => x.DateOfBirth).Must(d => d == null || d < DateTime.UtcNow.AddYears(-13)).WithMessage("Must be 13+.");
    }
}
public class LoginRequestValidator : AbstractValidator<LoginRequestDto>
{
    public LoginRequestValidator() { RuleFor(x => x.UsernameOrEmail).NotEmpty(); RuleFor(x => x.Password).NotEmpty(); }
}
public class ForgotPasswordValidator : AbstractValidator<ForgotPasswordRequestDto>
{
    public ForgotPasswordValidator() { RuleFor(x => x.Email).NotEmpty().EmailAddress(); }
}
public class ResetPasswordValidator : AbstractValidator<ResetPasswordRequestDto>
{
    public ResetPasswordValidator()
    {
        RuleFor(x => x.Token).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8);
        RuleFor(x => x.ConfirmNewPassword).Equal(x => x.NewPassword).WithMessage("Passwords must match.");
    }
}
public class ChangePasswordValidator : AbstractValidator<ChangePasswordRequestDto>
{
    public ChangePasswordValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8).NotEqual(x => x.CurrentPassword).WithMessage("Must differ from current.");
        RuleFor(x => x.ConfirmNewPassword).Equal(x => x.NewPassword).WithMessage("Passwords must match.");
    }
}
