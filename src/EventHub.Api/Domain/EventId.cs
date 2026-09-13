namespace EventHub.Api.Domain
{
    public readonly record struct EventId(Guid value)
    {
        public static EventId New() => new(Guid.CreateVersion7());
    }
}
