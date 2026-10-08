using System.Text;
using FluentValidation;
using UniProject.Share.Infrastructure.Constants;

namespace UniProject.API.Modules.Identity
{
    internal static class PasswordRuleExtensions
    {
        /// <summary>
        /// BCrypt chỉ dùng 72 byte đầu (UTF-8) của mật khẩu, phần sau bị bỏ qua.
        /// Chặn ở đây để hai mật khẩu khác nhau phía sau không bị coi là trùng.
        /// </summary>
        public const int MaxPasswordBytes = 72;

        public static IRuleBuilderOptions<T, string> MustFitBCrypt<T>(this IRuleBuilder<T, string> rule)
        {
            return rule
                .Must(p => p is null || Encoding.UTF8.GetByteCount(p) <= MaxPasswordBytes)
                .WithMessage($"Mật khẩu tối đa {MaxPasswordBytes} byte (khoảng 72 ký tự không dấu; ký tự có dấu chiếm 2-3 byte).");
        }
    }

    public class LoginRequestValidator : AbstractValidator<LoginRequest>
    {
        public LoginRequestValidator()
        {
            RuleFor(x => x.Username)
                .NotEmpty().WithMessage("Tên đăng nhập không được để trống.")
                .MaximumLength(50).WithMessage("Tên đăng nhập tối đa 50 ký tự.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Mật khẩu không được để trống.");
        }
    }

    public class CreateAccountRequestValidator : AbstractValidator<CreateAccountRequest>
    {
        public CreateAccountRequestValidator()
        {
            RuleFor(x => x.Username)
                .NotEmpty().WithMessage("Tên đăng nhập không được để trống.")
                .MinimumLength(3).WithMessage("Tên đăng nhập ít nhất 3 ký tự.")
                .MaximumLength(50).WithMessage("Tên đăng nhập tối đa 50 ký tự.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Mật khẩu không được để trống.")
                .MinimumLength(6).WithMessage("Mật khẩu tối thiểu 6 ký tự.")
                .MustFitBCrypt();

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email không được để trống.")
                .EmailAddress().WithMessage("Email không đúng định dạng.")
                .MaximumLength(100).WithMessage("Email tối đa 100 ký tự.");

            RuleFor(x => x.FullName)
                .NotEmpty().WithMessage("Họ tên không được để trống.")
                .MaximumLength(100).WithMessage("Họ tên tối đa 100 ký tự.");

            RuleFor(x => x.Role)
                .NotEmpty().WithMessage("Vai trò không được để trống.")
                .Must(role => AppRoles.All.Contains(role))
                .WithMessage($"Vai trò không hợp lệ. Các vai trò hợp lệ: {string.Join(", ", AppRoles.All)}");
        }
    }

    public class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
    {
        public ResetPasswordRequestValidator()
        {
            RuleFor(x => x.NewPassword)
                .NotEmpty().WithMessage("Mật khẩu mới không được để trống.")
                .MinimumLength(6).WithMessage("Mật khẩu mới tối thiểu 6 ký tự.")
                .MustFitBCrypt();
        }
    }

    public class AssignRoleRequestValidator : AbstractValidator<AssignRoleRequest>
    {
        public AssignRoleRequestValidator()
        {
            RuleFor(x => x.Role)
                .Must(role => AppRoles.All.Contains(role))
                .WithMessage($"Vai trò không hợp lệ. Các vai trò hợp lệ: {string.Join(", ", AppRoles.All)}");
        }
    }

    public class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequest>
    {
        public RefreshTokenRequestValidator()
        {
            RuleFor(x => x.AccessToken).NotEmpty().WithMessage("Thiếu access token.");
            RuleFor(x => x.RefreshToken).NotEmpty().WithMessage("Thiếu refresh token.");
        }
    }

    public class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
    {
        public ChangePasswordRequestValidator()
        {
            RuleFor(x => x.OldPassword)
                .NotEmpty().WithMessage("Mật khẩu cũ không được để trống.");

            RuleFor(x => x.NewPassword)
                .NotEmpty().WithMessage("Mật khẩu mới không được để trống.")
                .MinimumLength(6).WithMessage("Mật khẩu mới tối thiểu 6 ký tự.")
                .MustFitBCrypt()
                .NotEqual(x => x.OldPassword).WithMessage("Mật khẩu mới không được trùng với mật khẩu cũ.");
        }
    }
}
