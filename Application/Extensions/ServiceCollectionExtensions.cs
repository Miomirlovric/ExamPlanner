using Application.ExternalApi;
using Application.ExternalApi.Adapters;
using Application.ExternalApi.QuestionTypeStrategies;

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
        services.AddHttpClient(nameof(SortClient));
        services.AddHttpClient(nameof(SccClient));
        services.AddHttpClient(nameof(PathsClient));

        services.AddSingleton(new ExternalApiSettings { BaseUrl = apiBaseUrl });

        services.AddTransient(sp =>
            new CentralitiesClient(apiBaseUrl, sp.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(CentralitiesClient))));
        services.AddTransient(sp =>
            new PropertiesClient(apiBaseUrl, sp.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(PropertiesClient))));
        services.AddTransient(sp =>
            new GenerateClient(apiBaseUrl, sp.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(GenerateClient))));
        services.AddTransient(sp =>
            new ImageClient(apiBaseUrl, sp.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(ImageClient))));
        services.AddTransient(sp =>
            new SortClient(apiBaseUrl, sp.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(SortClient))));
        services.AddTransient(sp =>
            new SccClient(apiBaseUrl, sp.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(SccClient))));
        services.AddTransient(sp =>
            new PathsClient(apiBaseUrl, sp.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(PathsClient))));

        services.AddTransient<IGraphAnalysisService, GraphAnalysisService>();

        services.AddTransient<IAnswerAdapter<CentralitiesResponse>, CentralitiesAdapter>();
        services.AddTransient<IAnswerAdapter<PropertiesResponse>, PropertiesAdapter>();
        services.AddTransient<IAnswerAdapter<TopologicalSortResponse>, TopologicalSortAdapter>();
        services.AddTransient<IAnswerAdapter<StronglyConnectedComponentsResponse>, SccAdapter>();
        services.AddTransient<IAnswerAdapter<ShortestPathsResponse>, ShortestPathsAdapter>();

        services.AddTransient<IQuestionTypeStrategy, CentralitiesQuestionStrategy>();
        services.AddTransient<IQuestionTypeStrategy, PropertiesQuestionStrategy>();
        services.AddTransient<IQuestionTypeStrategy, TopologicalSortQuestionStrategy>();
        services.AddTransient<IQuestionTypeStrategy, SccQuestionStrategy>();
        services.AddTransient<IQuestionTypeStrategy, DijkstraQuestionStrategy>();

        services.AddTransient<IGraphQuestionservice, GraphQuestionservice>();

        return services;
    }
}
