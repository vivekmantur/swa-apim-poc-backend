using Microsoft.Extensions.DependencyInjection;
using SwaApimPoc.Application.Abstractions;
using SwaApimPoc.Infrastructure.Graph;
using SwaApimPoc.Infrastructure.Identity;
using SwaApimPoc.Infrastructure.Workflows;

namespace SwaApimPoc.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddHttpClient<IGraphProfileClient, GraphProfileClient>(client =>
        {
            client.BaseAddress = new Uri("https://graph.microsoft.com/v1.0/");
            client.Timeout = TimeSpan.FromSeconds(15);
        });

        services.AddSingleton<IVerifiedIdentityStore, InMemoryVerifiedIdentityStore>();
        services.AddSingleton<IWorkflowItemRepository, InMemoryWorkflowItemRepository>();
        return services;
    }
}
