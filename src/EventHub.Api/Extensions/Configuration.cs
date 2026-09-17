using EventHub.Api.Domain;
using EventHub.Api.Middleware;
using EventHub.Api.Service;

using Microsoft.AspNetCore.RateLimiting;

namespace EventHub.Api.Extensions;

public static class Configuration
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddValidation();
        services.AddOpenApi();
        services.AddScoped<IEventService, EventService>();
        services.AddOptions<BookingOptions>()
        .Bind(configuration.GetSection(BookingOptions.SectionName))
        .ValidateDataAnnotations()
        .ValidateOnStart();

        services.AddCors(o => o.AddPolicy("spa", p => p
            .WithOrigins("http://localhost:5213")
            .AllowAnyHeader()
            .AllowAnyMethod()));

        services.AddRateLimiter(o =>
        {
            o.AddFixedWindowLimiter("fixed", opt =>
            {
                opt.PermitLimit = 100;
                opt.Window = TimeSpan.FromMinutes(1);
            });
        });
        return services;
    }
}
