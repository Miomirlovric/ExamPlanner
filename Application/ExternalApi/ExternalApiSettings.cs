using static System.Net.WebRequestMethods;

namespace Application.ExternalApi;

public class ExternalApiSettings
{
    public const string SectionName = "ExternalApi";
    public string BaseUrl { get; set; } = "http://127.0.0.1:8000";
}
