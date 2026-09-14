using EventHub.Api.Domain;
using EventHub.Api.Examples;
using EventHub.Api.Extensions;
using EventHub.Api.Service;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddScoped<IEventService, EventService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapGet("/ping", () => "pong");

// The events of the next 7 days, sorted by the start time.
app.MapGet("/events", async (IEventService eventService, CancellationToken ct) => {
    var eventsTask = eventService.GetAllAsync(ct);
    var usersTask = UserService.GetAllAsync(ct);
    await Task.WhenAll(eventsTask, usersTask);
    var events = await eventsTask;
    var users = await usersTask;
    return Results.Ok(events);
});

app.Run();
