using Microsoft.Extensions.DependencyInjection;
using SwaApimPoc.Application.Dashboard;
using SwaApimPoc.Application.Graph;
using SwaApimPoc.Application.Identity;

namespace SwaApimPoc.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<DashboardService>();
        services.AddScoped<IdentityVerificationService>();
        services.AddScoped<GraphProbeService>();
        return services;
    }
}
