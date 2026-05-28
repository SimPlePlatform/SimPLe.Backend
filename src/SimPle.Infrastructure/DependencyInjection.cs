using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SimPle.Application.Common.Interfaces;
using SimPle.Application.Common.Options;
using SimPle.Infrastructure.Auth;
using SimPle.Infrastructure.Email;
using SimPle.Infrastructure.Persistence;
using SimPle.Infrastructure.Persistence.Repositories;

namespace SimPle.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.Configure<AuthOptions>(configuration.GetSection(AuthOptions.SectionName));
        services.Configure<RecaptchaOptions>(configuration.GetSection(RecaptchaOptions.SectionName));
        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));
        services.Configure<GoogleOptions>(configuration.GetSection(GoogleOptions.SectionName));

        services.AddScoped<IPasswordHashingService, Argon2PasswordHasher>();
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IEmailVerificationTokenRepository, EmailVerificationTokenRepository>();
        services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();
        services.AddScoped<IEmailService, SmtpEmailService>();
        services.AddScoped<IGoogleTokenValidationService, GoogleTokenValidationService>();
        services.AddScoped<IProfileRepository, ProfileRepository>();
        services.AddHttpClient<ICaptchaVerificationService, GoogleRecaptchaV2Service>();

        services.Configure<TokenCleanupOptions>(
            configuration.GetSection(TokenCleanupOptions.SectionName));
        services.AddHostedService<TokenCleanupService>();

        return services;
    }
}
