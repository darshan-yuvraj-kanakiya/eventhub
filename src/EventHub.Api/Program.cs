using EventHub.Api.Domain;
using EventHub.Api.Examples;
using EventHub.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapGet("/ping", () => "pong");

var events = LinqExamples.CreateEvents();


// The events of the next 7 days, sorted by the start time.
app.MapGet("/events", () => {
    var now = DateTime.Now;
    return events.Where(e => e.StartsAt >= now && e.StartsAt <= now.AddDays(7)).OrderBy(e => e.StartsAt).ToList();
});

// The count of events for each month.
app.MapGet("/groupEventsByMonth", () => {
    return events.GroupBy(e => e.StartsAt.Month).Select(group => new { Month = group.Key, Events = group.Count()}).ToList();
});

//The event with the fewest free seats.
app.MapGet("/fewSeats", () => {
    return events.Where(e => e.AvailableSeats <= 50).ToList();
});

//A page of 5 events, page number 2.
app.MapGet("/paginated", () => {
    return events.AsQueryable().Page(1, 5).ToList();
});

app.Run();
