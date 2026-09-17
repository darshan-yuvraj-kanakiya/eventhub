using System.Runtime.CompilerServices;

using EventHub.Api.Domain;
using EventHub.Api.Examples;

namespace EventHub.Api.Service;

public class EventService : IEventService
{
    private readonly List<Event> _events = LinqExamples.CreateEvents();

    public async Task<Event> Create(CreateEventDto createEventDto, CancellationToken ct)
    {
        await Task.Delay(500, ct);

        Event newEvent = new()
        {
            Title = createEventDto.Title,
            StartsAt = createEventDto.StartsAt,
            TotalSeats = createEventDto.TotalSeats
        };

        _events.Add(newEvent);

        return newEvent;
    }

    public async Task<IReadOnlyList<Event>> GetAllAsync(CancellationToken ct)
    {
        await Task.Delay(1000, ct);
        return _events;
    }

    public async Task<Event> GetAsync(Domain.EventId id, CancellationToken ct)
    {
        await Task.Delay(100, ct);
        var @event = _events.FirstOrDefault(e => e.Id == id);
        return @event
            ?? throw new ArgumentNullException("event not found");
    }

    public async IAsyncEnumerable<Event> StreamAsync([EnumeratorCancellation] CancellationToken ct)
    {
        foreach(var e in _events)
        {
            ct.ThrowIfCancellationRequested();
            await Task.Delay(50, ct);
            yield return e;
        }
    }
}


public record CreateEventDto
{
    public required string Title { get; set; }
    public required DateTimeOffset StartsAt { get; set; }
    public required int TotalSeats { get; set; }
}