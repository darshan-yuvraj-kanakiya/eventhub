using System.ComponentModel.DataAnnotations;

using Microsoft.Extensions.Options;

namespace EventHub.Api.Domain;

public sealed class BookingOptions
{
    public const string SectionName = "Booking";

    [Range(1, 100)]
    public int MaxSeatsPerBooking { get; init; } = 10;

    [Required]
    public string SupportEmail { get; init; } = "abc@gmail.com";
}

public class BookingService(IOptions<BookingOptions> options)
{
    private readonly BookingOptions _options = options.Value;

    public int GetSeatsPerBooking()
    {
        return _options.MaxSeatsPerBooking;
    }
}
