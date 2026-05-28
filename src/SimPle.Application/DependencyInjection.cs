using Microsoft.Extensions.DependencyInjection;
using SimPle.Application.Auth.Services;

namespace SimPle.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();

        return services;
    }
}
