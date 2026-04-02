using Application.ExternalApi;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, string apiBaseUrl)
    {
        services.AddHttpClient(nameof(CentralitiesClient));
        services.AddHttpClient(nameof(PropertiesClient));
        services.AddHttpClient(nameof(GenerateClient));
        services.AddHttpClient(nameof(ImageClient));

        services.AddSingleton(new ExternalApiSettings { BaseUrl = apiBaseUrl });

        services.AddTransient(sp =>
            new CentralitiesClient(apiBaseUrl, sp.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(CentralitiesClient))));
        services.AddTransient(sp =>
            new PropertiesClient(apiBaseUrl, sp.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(PropertiesClient))));
        services.AddTransient(sp =>
            new GenerateClient(apiBaseUrl, sp.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(GenerateClient))));
        services.AddTransient(sp =>
            new ImageClient(apiBaseUrl, sp.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(ImageClient))));

        services.AddTransient<IGraphAnalysisService, GraphAnalysisService>();
        services.AddTransient<IGraphSectionService, GraphSectionService>();

        return services;
    }
}
