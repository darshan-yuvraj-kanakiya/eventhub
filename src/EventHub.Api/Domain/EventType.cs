namespace EventHub.Api.Domain
{
    public readonly record struct EventId(Guid value)
    {
        public static EventId New() => new(Guid.CreateVersion7());
    }

    public record Money(decimal Amount, string Currency);

    public enum BookingStatus { Pending, Confirmed, Cancelled }

    public class Event
    {
        public EventId Id { get; private set; } = EventId.New();
        public required string Title { get; set; }
        public required DateTimeOffset StartsAt { get; set; }
        public required int TotalSeats { get; set; }
        public int BookedSeats { get; private set; }

        public int AvailableSeats => TotalSeats - BookedSeats;

        // Write a switch expression that gives a text status for an event: "cancelled", "sold out", "closing soon", or "open".
        public string GetEventStatus()
        {
            return this switch
            {
                { AvailableSeats: 0 } => "sold out",
                { BookedSeats: 0, StartsAt: var s } when s.CompareTo(DateTimeOffset.Now) > 0 => "cancelled",
                { StartsAt: var s } when s.CompareTo(DateTimeOffset.Now) < 0 => "closing soon",
                _ => "open"
            };

        }

        public void BookSeat(int seats)
        {
            BookedSeats = seats;
        }
    }
}
