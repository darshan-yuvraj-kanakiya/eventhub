using EventHub.Api.Domain;
using EventHub.Api.Service;

namespace EventHub.Api.Extensions;

public static class Configuration
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOpenApi();
        services.AddScoped<IEventService, EventService>();
        services.AddOptions<BookingOptions>()
        .Bind(configuration.GetSection(BookingOptions.SectionName))
        .ValidateDataAnnotations()
        .ValidateOnStart();
        return services;
    }
}
