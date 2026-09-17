using EventHub.Api.Domain;
using EventHub.Api.Service;

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace EventHub.Api.Endpoints;

public static class EventEndpoints
{
    public static void MapEventEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/events")
                       .WithTags("Events")
                       .RequireRateLimiting("fixed");

        group.MapGet("/", GetAll);
        group.MapGet("{id:guid}", GetById);
        group.MapPost("/", Create).AllowAnonymous();
    }

    private static async Task<Ok<IReadOnlyList<Event>>> GetAll(IEventService eventService, CancellationToken ct)
    {
        var items = await eventService.GetAllAsync(ct);
        return TypedResults.Ok(items);
    }

    private static async Task<Results<Ok<Event>, NotFound>> GetById(Guid id, IEventService eventService, CancellationToken ct)
    {
        var eventId = new Domain.EventId(id);
        var item = await eventService.GetAsync(eventId, ct);
        return item is null ? TypedResults.NotFound() : TypedResults.Ok(item);
    }

    private static async Task<Ok<Event>> Create([FromBody] CreateEventDto eventRequest, IEventService eventService, CancellationToken ct)
    {
        var item = await eventService.Create(eventRequest, ct);
        return TypedResults.Ok(item);
    }
}
