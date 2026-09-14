using System.Runtime.CompilerServices;

using EventHub.Api.Domain;
using EventHub.Api.Examples;

namespace EventHub.Api.Service;

public class EventService : IEventService
{
    private readonly List<Event> _events = LinqExamples.CreateEvents();

    public async Task<List<Event>> GetAllAsync(CancellationToken ct)
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
