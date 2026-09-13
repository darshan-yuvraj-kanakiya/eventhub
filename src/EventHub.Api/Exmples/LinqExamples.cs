using EventHub.Api.Domain;

namespace EventHub.Api.Examples;

public static class LinqExamples
{
    public static List<Event> CreateEvents()
    {
        var now = DateTimeOffset.UtcNow;

        return
        [
            new Event
            {
                Title = "C# Fundamentals",
                StartsAt = now.AddDays(1),
                TotalSeats = 100
            },

            new Event
            {
                Title = "ASP.NET Core",
                StartsAt = now.AddDays(2),
                TotalSeats = 80
            },

            new Event
            {
                Title = "React Basics",
                StartsAt = now.AddDays(3),
                TotalSeats = 60
            },

            new Event
            {
                Title = "TypeScript",
                StartsAt = now.AddDays(4),
                TotalSeats = 70
            },

            new Event
            {
                Title = "Clean Architecture",
                StartsAt = now.AddDays(5),
                TotalSeats = 50
            },

            new Event
            {
                Title = "Entity Framework Core",
                StartsAt = now.AddDays(6),
                TotalSeats = 90
            },

            new Event
            {
                Title = "LINQ Deep Dive",
                StartsAt = now.AddDays(7),
                TotalSeats = 40
            },

            new Event
            {
                Title = "Docker for Developers",
                StartsAt = now.AddDays(8),
                TotalSeats = 100
            },

            new Event
            {
                Title = "REST API Design",
                StartsAt = now.AddDays(9),
                TotalSeats = 80
            },

            new Event
            {
                Title = "Authentication",
                StartsAt = now.AddDays(10),
                TotalSeats = 70
            },

            new Event
            {
                Title = "Authorization",
                StartsAt = now.AddDays(11),
                TotalSeats = 70
            },

            new Event
            {
                Title = "Testing in .NET",
                StartsAt = now.AddDays(12),
                TotalSeats = 60
            },

            new Event
            {
                Title = "Integration Testing",
                StartsAt = now.AddDays(13),
                TotalSeats = 50
            },

            new Event
            {
                Title = "Performance",
                StartsAt = now.AddDays(14),
                TotalSeats = 40
            },

            new Event
            {
                Title = "Caching",
                StartsAt = now.AddDays(15),
                TotalSeats = 100
            },

            new Event
            {
                Title = "Message Queues",
                StartsAt = now.AddDays(16),
                TotalSeats = 80
            },

            new Event
            {
                Title = "Microservices",
                StartsAt = now.AddDays(17),
                TotalSeats = 120
            },

            new Event
            {
                Title = "Observability",
                StartsAt = now.AddDays(18),
                TotalSeats = 90
            },

            new Event
            {
                Title = "CI/CD",
                StartsAt = now.AddDays(19),
                TotalSeats = 100
            },

            new Event
            {
                Title = "Cloud Deployment",
                StartsAt = now.AddDays(20),
                TotalSeats = 80
            }
        ];
    }
}