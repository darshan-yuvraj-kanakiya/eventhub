using EventHub.Api.Domain;

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

app.MapGet("/event", () =>
{
    var testEvent = new Event{ StartsAt = DateTimeOffset.Now.AddMinutes(5), Title = "Tomorrow Land", TotalSeats = 1};
    testEvent.BookSeat(2);
    return testEvent.GetEventStatus();
});

app.Run();
