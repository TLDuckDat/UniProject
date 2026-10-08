using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace UniProject.API.Modules.Identity
{
    public static class IdentityModule
    {
        public static IServiceCollection AddIdentityModule(this IServiceCollection services)
        {
            services.AddScoped<IPasswordHasher, PasswordHasher>();
            services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
            services.AddScoped<IIdentityService, IdentityService>();

            services.AddScoped<IValidator<LoginRequest>, LoginRequestValidator>();
            services.AddScoped<IValidator<CreateAccountRequest>, CreateAccountRequestValidator>();
            services.AddScoped<IValidator<ChangePasswordRequest>, ChangePasswordRequestValidator>();
            services.AddScoped<IValidator<ResetPasswordRequest>, ResetPasswordRequestValidator>();
            services.AddScoped<IValidator<AssignRoleRequest>, AssignRoleRequestValidator>();
            services.AddScoped<IValidator<RefreshTokenRequest>, RefreshTokenRequestValidator>();

            return services;
        }
    }
}
