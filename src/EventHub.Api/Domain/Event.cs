namespace EventHub.Api.Domain
{
    public class Event
    {
        public EventId Id { get; private set; } = EventId.New();
        public required string Title { get; set; }
        public required DateTimeOffset StartsAt { get; set; }
        public required int TotalSeats { get; set; }
        public int BookedSeats { get; private set; }
        public bool IsCancelled { get; private set; }

        public int AvailableSeats => TotalSeats - BookedSeats;

        public string GetEventStatus()
        {
            return this switch
            {
                { IsCancelled: true} => "cancelled",
                { AvailableSeats: 0 } => "sold out",
                { AvailableSeats: <= 10 } => "closing soon",
                _ => "open"
            };

        }

        public void BookSeats(int seats)
        {
            if (seats <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(seats));
            }

            if (seats > AvailableSeats)
            {
                throw new InvalidOperationException("Not enough available seats");
            }

            BookedSeats += seats;
        }

        public void Cancle()
        {
            IsCancelled = true;
        }
    }
}
