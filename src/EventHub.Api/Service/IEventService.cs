using EventHub.Api.Domain;

namespace EventHub.Api.Service;

public interface IEventService
{
    Task<Event> GetAsync(Domain.EventId id, CancellationToken ct);
    Task<List<Event>> GetAllAsync(CancellationToken ct);
    IAsyncEnumerable<Event> StreamAsync(CancellationToken ct);
 }