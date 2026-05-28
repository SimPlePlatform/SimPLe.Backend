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
using SimPle.Infrastructure.Storage;

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
        services.AddScoped<IUsernameChangeRequestRepository, UsernameChangeRequestRepository>();
        services.Configure<AwsOptions>(configuration.GetSection(AwsOptions.SectionName));
        services.PostConfigure<AwsOptions>(options =>
        {
            ApplyAwsEnvironmentFallbacks(options, configuration);
        });
        services.AddScoped<IFileStorageService, S3FileStorageService>();
        services.AddHttpClient<ICaptchaVerificationService, GoogleRecaptchaV2Service>();

        services.Configure<TokenCleanupOptions>(
            configuration.GetSection(TokenCleanupOptions.SectionName));
        services.AddHostedService<TokenCleanupService>();

        return services;
    }

    private static void ApplyAwsEnvironmentFallbacks(AwsOptions options, IConfiguration configuration)
    {
        options.Region = configuration["AWS_REGION"] ?? options.Region;
        options.S3BucketName = configuration["AWS_S3_BUCKET_NAME"] ?? options.S3BucketName;
        options.S3ProfilePrefix = configuration["AWS_S3_PROFILE_PREFIX"] ?? options.S3ProfilePrefix;
        if (int.TryParse(configuration["AWS_S3_UPLOAD_URL_EXPIRY_MINUTES"], out var upload))
            options.S3UploadUrlExpiryMinutes = upload;
        if (int.TryParse(configuration["AWS_S3_READ_URL_EXPIRY_MINUTES"], out var read))
            options.S3ReadUrlExpiryMinutes = read;
    }
}
