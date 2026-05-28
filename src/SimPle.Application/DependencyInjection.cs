using Microsoft.Extensions.DependencyInjection;
using SimPle.Application.Auth.Services;
using SimPle.Application.Profiles.Services;

namespace SimPle.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IProfileService, ProfileService>();

        return services;
    }
}
