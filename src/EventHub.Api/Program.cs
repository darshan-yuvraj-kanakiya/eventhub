using EventHub.Api.Domain;
using EventHub.Api.Endpoints;
using EventHub.Api.Extensions;
using EventHub.Api.Middleware;
using EventHub.Api.Service;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateScopes = true;
    options.ValidateOnBuild = true;
});

builder.Services.AddApplication(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseMiddleware<CorrelationIdMiddleware>();

app.UseCors("spa");
app.UseRateLimiter();

app.UseHttpsRedirection();

app.MapGet("/ping", () => "pong");

// The events of the next 7 days, sorted by the start time.
app.MapEventEndpoints();

app.Run();
