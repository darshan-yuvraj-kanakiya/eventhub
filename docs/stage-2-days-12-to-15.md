# Stage 2 — The modular monolith (Day 12 to Day 15)

Stage 1 gave you one API with clean layers. That is good, but it has one problem. All the code can call all the other code. In two years, the Bookings code will call the Events database tables directly. The layers stay, but the features become one large lump.

A modular monolith solves this. It is one process, one deployment, and one repository. Inside, the modules cannot see each other, except through a small public contract.

**This is the correct architecture for almost all systems.** Learn it well. Microservices in Stage 3 are for the cases where a modular monolith is not sufficient.

---

# Day 12 — Module boundaries

## Why this day

A boundary is the most important decision in a system. A wrong boundary costs more than any other mistake. A modular monolith lets you move a boundary cheaply. A microservice does not.

## Concepts

### What is a module?

A module owns:
- Its business rules.
- Its data. No other module reads its tables.
- Its public contract. This is the only thing that other modules see.

A module does **not** own:
- The host process. All modules share one.
- The web server, the logging, and the configuration.

### How to find a boundary

Do not divide by technical layer. `Controllers`, `Services`, and `Repositories` are not modules.

Divide by **business capability**. Ask these questions:
- Which words does the business use? "Event", "Booking", "Notification", "Payment". These are candidates.
- Which parts change together? If two parts always change in the same pull request, they are one module.
- Who owns the part? One team should own a module.
- What data does the part own? Two modules must not write the same table.

For EventHub:

| Module | Owns | Public contract |
|---|---|---|
| **Events** | The event, the seats, the schedule | `EventDto`, `IEventReader`, `SeatsReserved` event |
| **Bookings** | The booking, the status, the cancellation | `BookingDto`, `BookingConfirmed` event |
| **Notifications** | The messages and the templates | Nothing public. It only listens. |
| **Users** | The account and the profile | `UserDto`, `IUserReader` |

Note that Notifications has no public contract. Nobody calls it. It reacts to events. This is a good sign of a clean boundary.

### The C# tools for a boundary

**1. The `internal` keyword.** A type that is `internal` is visible only inside its own assembly (its own project). This is your main tool.

```csharp
// EventHub.Modules.Events.Application
internal sealed class CreateEventHandler { }     // not visible outside
public sealed record EventDto(Guid Id, string Title);   // no! Put this in Contracts.
```

**2. Separate projects.** One module is many projects. Only one of them is public.

```
Modules/Events/
├─ EventHub.Modules.Events.Contracts/       ← PUBLIC. Other modules reference this only.
├─ EventHub.Modules.Events.Domain/          ← internal
├─ EventHub.Modules.Events.Application/     ← internal
└─ EventHub.Modules.Events.Infrastructure/  ← internal
```

**3. Architecture tests.** The compiler cannot stop a bad project reference that a developer adds. A test can.

```csharp
[Fact]
public void Bookings_must_not_reference_Events_internals()
{
    var result = Types.InAssembly(typeof(BookingsModule).Assembly)
        .ShouldNot()
        .HaveDependencyOnAny(
            "EventHub.Modules.Events.Domain",
            "EventHub.Modules.Events.Application",
            "EventHub.Modules.Events.Infrastructure")
        .GetResult();

    result.IsSuccessful.ShouldBeTrue(
        string.Join(", ", result.FailingTypeNames ?? []));
}
```

Write one test for each module pair. Run them in CI. They stop the slow decay.

### The database: one database, many schemas

Each module gets its own PostgreSQL schema. Nobody joins across a schema.

```csharp
// in the Events module
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.HasDefaultSchema("events");
    modelBuilder.ApplyConfigurationsFromAssembly(typeof(EventsDbContext).Assembly);
}
```

Each module has its own `DbContext` and its own migration history table:

```csharp
services.AddDbContext<EventsDbContext>(o => o
    .UseNpgsql(connectionString, npgsql =>
        npgsql.MigrationsHistoryTable("__ef_migrations_history", "events")));
```

This one rule gives you most of the value of microservices with none of the cost. When you split a module into a service later, the data is already separate. You only move the schema to a new database.

**Why is this hard?** Because you cannot write a join between `events.events` and `bookings.bookings`. You must ask the other module for the data, or you must keep a copy. This feels slow at first. It is the point of the exercise. A microservice has the same limit, and there you cannot cheat.

### Module registration

Each module registers itself. `Program.cs` stays small.

```csharp
// in the Events module
public static class EventsModule
{
    public static IServiceCollection AddEventsModule(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<EventsDbContext>(/* ... */);
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<IEventReader, EventReader>();     // the public contract
        return services;
    }

    public static IEndpointRouteBuilder MapEventsModule(this IEndpointRouteBuilder app)
    {
        app.MapEventEndpoints();
        return app;
    }
}

// Program.cs
builder.Services
    .AddEventsModule(builder.Configuration)
    .AddBookingsModule(builder.Configuration)
    .AddNotificationsModule(builder.Configuration);

app.MapEventsModule()
   .MapBookingsModule();
```

### How do modules talk?

Two methods. Choose by the question "do I need the answer now?".

**1. A synchronous call through a public interface.** Use it when you need data immediately.

```csharp
// in EventHub.Modules.Events.Contracts
public interface IEventReader
{
    Task<EventSummary?> GetAsync(Guid eventId, CancellationToken ct);
}

public sealed record EventSummary(Guid Id, string Title, int AvailableSeats);
```

The Bookings module injects `IEventReader`. It does not know the class. It cannot touch the Events database.

Keep the contract small. Return only what the other module needs. Do not return the full entity.

**2. An asynchronous integration event.** Use it when the other module must only know that something happened.

```csharp
// in EventHub.Modules.Bookings.Contracts
public sealed record BookingConfirmedIntegrationEvent(
    Guid BookingId,
    Guid EventId,
    Guid UserId,
    int Seats,
    DateTimeOffset OccurredAtUtc);
```

Bookings publishes it. Notifications and Events subscribe. Bookings does not know who listens.

### Domain event against integration event

This confuses many developers. Know the difference.

| | Domain event | Integration event |
|---|---|---|
| Scope | Inside one module | Between modules |
| Name | `BookingCreatedDomainEvent` | `BookingConfirmedIntegrationEvent` |
| Content | Rich. Can hold entities. | Flat. Only primitive data. |
| Timing | Inside the transaction, or just after | After the transaction commits |
| Contract | Private. Change it freely. | Public. Version it carefully. |

An integration event is an API. When you publish it, other teams depend on it. Add fields. Never remove a field or change its meaning.

## Build

Change the structure to this:

```
eventhub/
├─ src/
│  ├─ Api/
│  │  └─ EventHub.Api/                  ← the only host. It references the modules.
│  ├─ Modules/
│  │  ├─ Events/
│  │  │  ├─ EventHub.Modules.Events.Contracts/
│  │  │  ├─ EventHub.Modules.Events.Domain/
│  │  │  ├─ EventHub.Modules.Events.Application/
│  │  │  └─ EventHub.Modules.Events.Infrastructure/
│  │  ├─ Bookings/
│  │  │  └─ (the same four projects)
│  │  └─ Notifications/
│  │     └─ (the same four projects)
│  └─ Common/
│     ├─ EventHub.Common.Domain/         ← Entity, AggregateRoot, Result
│     ├─ EventHub.Common.Application/    ← behaviours, abstractions
│     └─ EventHub.Common.Infrastructure/ ← the outbox, the base DbContext, auth
└─ tests/
   ├─ Modules/Events/...
   ├─ Modules/Bookings/...
   └─ EventHub.ArchitectureTests/
```

Steps:
1. Make the projects. Add them to the solution with solution folders.
2. Move the event code to the Events module. Move the booking code to the Bookings module.
3. Give a schema to each module. Make a separate `DbContext` for each.
4. Make a new migration for each module. Note the two history tables.
5. Find each place where Bookings reads an event. Replace it with `IEventReader`.
6. Make all types `internal`, except the Contracts project.
7. Write the architecture tests.
8. Confirm that the application still works from end to end.

## Common mistakes

- You make a "Shared" module and put everything in it. Then it becomes the new lump. Only put technical base types in Common. Never put a business entity there.
- You reference the Events `DbContext` from Bookings "for one query". This destroys the boundary. Add a method to the contract instead.
- You make 12 modules for a small system. Start with 2 or 3. Divide when you feel the pain.

## Check

- Bookings needs the event title on the booking list screen. Give two solutions. Which is better and why?
- Which projects can the Notifications module reference?

---

# Day 13 — Background work and the outbox pattern

## Why this day

You now publish integration events. But a publish can fail after the data is saved. Then the data and the events do not agree. The outbox pattern solves this. It is the most important pattern in distributed systems, and it also applies in a monolith.

## Concepts

### Background services

A `BackgroundService` runs for the life of the application, next to the web server.

```csharp
public sealed class OutboxProcessor(
    IServiceScopeFactory scopeFactory,
    ILogger<OutboxProcessor> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await ProcessAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "The outbox processing failed.");
                // Do not throw. The service must continue.
            }
        }
    }
}
```

Important rules:
1. **Make a scope.** A `BackgroundService` is a singleton. A `DbContext` is scoped. Use `IServiceScopeFactory`.
2. **Catch all exceptions.** An unhandled exception in `ExecuteAsync` can stop the whole application.
3. **Respect the stopping token.** Send it to all asynchronous calls. The application must stop cleanly.
4. **Do not block the start.** If `ExecuteAsync` has no `await` before the loop, the host waits. Put `await Task.Yield()` first if necessary.

Register it:

```csharp
builder.Services.AddHostedService<OutboxProcessor>();
```

### Channels: a queue in memory

`System.Threading.Channels` gives a producer and a consumer inside one process.

```csharp
var channel = Channel.CreateBounded<EmailMessage>(new BoundedChannelOptions(1000)
{
    FullMode = BoundedChannelFullMode.Wait
});

// producer
await channel.Writer.WriteAsync(message, ct);

// consumer, in a BackgroundService
await foreach (var message in channel.Reader.ReadAllAsync(ct))
{
    await SendAsync(message, ct);
}
```

Use a **bounded** channel. An unbounded channel can use all the memory if the consumer is slow.

Warning: a channel is in memory. If the process stops, the messages are lost. Use it only for work that you can lose, such as a metric or a cache refresh. Never use it for a business event.

### The problem that the outbox solves

```csharp
await context.SaveChangesAsync(ct);          // the booking is saved
await messageBus.PublishAsync(evt, ct);      // ← the process stops here
```

Result: the booking exists, but no notification is sent, and the seat count is not updated. The system is now wrong, and nothing tells you.

The other order is also wrong:

```csharp
await messageBus.PublishAsync(evt, ct);      // the notification is sent
await context.SaveChangesAsync(ct);          // ← this fails
```

Result: the user gets an email for a booking that does not exist.

You cannot make one transaction across a database and a message broker. So you use the outbox.

### The outbox pattern

**The idea:** write the message to a table in the same transaction as the data. A separate process reads the table and sends the messages.

The table:

```csharp
public sealed class OutboxMessage
{
    public Guid Id { get; init; }
    public required string Type { get; init; }          // the full type name
    public required string Content { get; init; }       // the JSON payload
    public DateTimeOffset OccurredOnUtc { get; init; }
    public DateTimeOffset? ProcessedOnUtc { get; set; }
    public string? Error { get; set; }
    public int Attempts { get; set; }
}
```

Write it in `SaveChangesAsync`:

```csharp
public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
{
    var outboxMessages = ChangeTracker
        .Entries<IAggregateRoot>()
        .SelectMany(entry =>
        {
            var events = entry.Entity.DomainEvents;
            entry.Entity.ClearDomainEvents();
            return events;
        })
        .Select(domainEvent => new OutboxMessage
        {
            Id = Guid.CreateVersion7(),
            Type = domainEvent.GetType().AssemblyQualifiedName!,
            Content = JsonSerializer.Serialize(domainEvent, domainEvent.GetType()),
            OccurredOnUtc = timeProvider.GetUtcNow()
        })
        .ToList();

    AddRange(outboxMessages);

    return await base.SaveChangesAsync(ct);   // one transaction for the data and the messages
}
```

Read it in the background service:

```csharp
private async Task ProcessAsync(CancellationToken ct)
{
    using var scope = scopeFactory.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

    var messages = await context.Set<OutboxMessage>()
        .Where(m => m.ProcessedOnUtc == null && m.Attempts < 5)
        .OrderBy(m => m.OccurredOnUtc)
        .Take(20)
        .ToListAsync(ct);

    foreach (var message in messages)
    {
        try
        {
            var type = Type.GetType(message.Type)!;
            var payload = JsonSerializer.Deserialize(message.Content, type)!;

            await publisher.PublishAsync(payload, ct);

            message.ProcessedOnUtc = timeProvider.GetUtcNow();
        }
        catch (Exception ex)
        {
            message.Attempts++;
            message.Error = ex.ToString();
            logger.LogError(ex, "The outbox message {MessageId} failed.", message.Id);
        }
    }

    await context.SaveChangesAsync(ct);
}
```

**The guarantee:** at-least-once delivery. A message is sent one time or more. It is never lost. This leads to the next rule.

### Idempotency

Because a message can arrive two times, a handler must give the same result each time.

Method 1: a table of handled messages.

```csharp
public sealed class InboxMessage
{
    public Guid MessageId { get; init; }
    public required string HandlerName { get; init; }
    public DateTimeOffset HandledOnUtc { get; init; }
}

// in the handler
var alreadyHandled = await context.Set<InboxMessage>()
    .AnyAsync(m => m.MessageId == evt.Id && m.HandlerName == handlerName, ct);

if (alreadyHandled)
{
    return;
}
```

Method 2: make the operation naturally idempotent.

```csharp
// NOT idempotent. Two messages give two extra seats.
evt.BookedSeats += seats;

// Idempotent. The result is the same each time.
evt.BookedSeats = totalFromBookingsTable;
```

Method 3: a unique constraint. Let the database refuse the duplicate. Catch the error and ignore it.

Always ask this question for each handler: "what happens if this runs two times?"

### Scheduled jobs

For work at a fixed time, a `PeriodicTimer` is often sufficient. For real scheduling, use Quartz.NET or Hangfire.

| | Quartz.NET | Hangfire |
|---|---|---|
| Free | Yes, fully | The core is free. Some features are paid. |
| Storage | Database or memory | Database |
| Dashboard | No | Yes, a good one |
| Cron | Yes | Yes |

For a monolith with a dashboard need, Hangfire is easy. For a pure open-source stack, use Quartz.NET.

**Warning about many instances.** If you run three copies of the API, the background service runs three times. Three copies of the outbox processor will send each message three times, or they will fight over the rows. Solutions:
- Use `FOR UPDATE SKIP LOCKED` in PostgreSQL. Each worker takes different rows.
- Use a distributed lock.
- Run the background work in a separate worker application. This is the cleanest method.

```sql
SELECT * FROM outbox_messages
WHERE processed_on_utc IS NULL
ORDER BY occurred_on_utc
LIMIT 20
FOR UPDATE SKIP LOCKED;
```

## Build

1. Add an `OutboxMessage` entity to the Common Infrastructure project. Give each module its own outbox table in its own schema.
2. Change `SaveChangesAsync` to write the domain events to the outbox.
3. Write the `OutboxProcessor` background service.
4. Publish `BookingConfirmedIntegrationEvent` through the outbox.
5. Handle it in Notifications. Write a log line. Handle it in Events. Update the seat count.
6. Make the Notifications handler idempotent with an inbox table.
7. Test the failure: stop the application between `SaveChanges` and the processing. Start it again. Confirm that the message is still sent.
8. Test the duplicate: run the same message two times by hand. Confirm that only one notification exists.
9. Add `FOR UPDATE SKIP LOCKED` with raw SQL. Run two copies of the application. Confirm that no message is sent two times.

## Common mistakes

- You forget the scope in a `BackgroundService`. You get "Cannot consume scoped service from singleton".
- Your outbox processor throws, and the loop stops. The messages stop with no warning. Always catch, log, and continue.
- You do not limit the attempts. A poison message blocks the queue forever. Add a maximum attempt count and a dead-letter state.
- You do not clear the domain events after you read them. They are published two times.

## Check

- Why can you not put the database write and the message publish in one transaction?
- Give three ways to make a handler idempotent.

---

# Day 14 — Caching, resilience, and speed

## Why this day

Your system now works. It must also be fast, and it must survive when a dependency fails.

## Concepts

### Caching: which level?

| Type | Where | Use |
|---|---|---|
| In-memory (`IMemoryCache`) | The process | Very fast. It is lost on restart. Each instance has its own. |
| Distributed (Redis) | A separate server | Shared between instances. It survives a restart. It is slower. |
| Hybrid (`HybridCache`) | Both | Level 1 in memory, level 2 in Redis. Use this. |
| Output caching | The full HTTP response | A public page with no user data. |
| Response caching | The client and the proxy | Static content. |

### HybridCache

`HybridCache` came in .NET 9. It replaces most of your cache code. It solves the "cache stampede" problem: 100 requests for a missing key start only one database query.

```csharp
builder.Services.AddHybridCache(options =>
{
    options.DefaultEntryOptions = new HybridCacheEntryOptions
    {
        Expiration = TimeSpan.FromMinutes(10),        // in Redis
        LocalCacheExpiration = TimeSpan.FromMinutes(1) // in memory
    };
});

builder.Services.AddStackExchangeRedisCache(o =>
    o.Configuration = builder.Configuration.GetConnectionString("Redis"));
```

Use it:

```csharp
public sealed class EventReader(HybridCache cache, EventsDbContext context) : IEventReader
{
    public async ValueTask<EventSummary?> GetAsync(Guid id, CancellationToken ct)
    {
        return await cache.GetOrCreateAsync(
            $"event:{id}",
            async token => await context.Events
                .AsNoTracking()
                .Where(e => e.Id == new EventId(id))
                .Select(e => new EventSummary(e.Id.Value, e.Title, e.AvailableSeats))
                .FirstOrDefaultAsync(token),
            cancellationToken: ct);
    }
}
```

Remove the entry when the data changes:

```csharp
await cache.RemoveAsync($"event:{id}", ct);
```

Or use tags to remove a group:

```csharp
await cache.GetOrCreateAsync(key, factory, tags: ["events"], cancellationToken: ct);
await cache.RemoveByTagAsync("events", ct);
```

### The rules of caching

1. **Do not cache data that changes often.** The seat count changes with each booking. Do not cache it, or cache it for 5 seconds only.
2. **Cache invalidation is hard.** Prefer a short expiration over a complex invalidation.
3. **A cache is not a database.** The application must work when the cache is empty.
4. **Never cache data for one user in a shared cache with no user key.** This leaks data between users. It is a serious bug.
5. **Measure first.** Do not add a cache before you know that the query is slow.

### HttpClient

Never write `new HttpClient()`. It holds a socket after disposal, and it does not see a DNS change.

Use a typed client:

```csharp
builder.Services.AddHttpClient<IPaymentGateway, PaymentGateway>(client =>
{
    client.BaseAddress = new Uri(options.PaymentApiUrl);
    client.Timeout = TimeSpan.FromSeconds(10);
});

public sealed class PaymentGateway(HttpClient httpClient) : IPaymentGateway
{
    public async Task<PaymentResult> ChargeAsync(ChargeRequest request, CancellationToken ct)
    {
        var response = await httpClient.PostAsJsonAsync("/charge", request, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PaymentResult>(ct))!;
    }
}
```

`IHttpClientFactory` manages the handler lifetime. It rotates the connections.

### Resilience

Add the standard resilience handler. It uses Polly under the surface.

```bash
dotnet add package Microsoft.Extensions.Http.Resilience
```

```csharp
builder.Services.AddHttpClient<IPaymentGateway, PaymentGateway>(/* ... */)
    .AddStandardResilienceHandler();
```

This one line gives you five things:
1. **Rate limiter** — it limits the parallel calls.
2. **Total timeout** — 30 seconds by default.
3. **Retry** — 3 attempts with exponential backoff and jitter.
4. **Circuit breaker** — it stops calls after many failures.
5. **Attempt timeout** — 10 seconds for each try.

To change the values:

```csharp
.AddStandardResilienceHandler(options =>
{
    options.Retry.MaxRetryAttempts = 3;
    options.Retry.Delay = TimeSpan.FromMilliseconds(500);
    options.CircuitBreaker.FailureRatio = 0.5;
    options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
    options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(5);
});
```

### Retry against circuit breaker

Many developers confuse these two. They solve different problems.

**Retry** solves a short problem. A packet was lost. A server was busy for 50 ms. Try again, and it works.

**Circuit breaker** solves a long problem. The payment service is down. If you retry, you add load to a dying service, and you hold your own threads. The circuit breaker opens after many failures, and it fails immediately for the next 30 seconds. Then it tries one call. If that works, it closes.

Without a circuit breaker, a retry policy makes an outage worse.

**Important:** only retry an idempotent operation. A retry on `POST /charge` can charge the customer two times. Send an idempotency key with the request.

### Rate limiting

```csharp
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("per-user", context =>
        RateLimitPartition.GetTokenBucketLimiter(
            context.User.Identity?.Name ?? context.Connection.RemoteIpAddress?.ToString() ?? "anon",
            _ => new TokenBucketRateLimiterOptions
            {
                TokenLimit = 100,
                TokensPerPeriod = 10,
                ReplenishmentPeriod = TimeSpan.FromSeconds(1),
                QueueLimit = 0
            }));
});

group.MapPost("/", Create).RequireRateLimiting("per-user");
```

Four algorithms: fixed window, sliding window, token bucket, and concurrency. The token bucket allows a short burst. It is usually the best choice for an API.

### Measure, do not guess

Use BenchmarkDotNet for a method:

```csharp
[MemoryDiagnoser]
public class MappingBenchmarks
{
    [Benchmark(Baseline = true)]
    public EventDto Manual() => _event.ToDto();

    [Benchmark]
    public EventDto Reflection() => _mapper.Map<EventDto>(_event);
}
```

For the API, use a load tool such as k6 or `bombardier`. Measure the p95 and the p99, not the average. The average hides the slow requests.

Common causes of slowness, in order:
1. A missing database index.
2. An N+1 query.
3. Too much data returned. Use paging.
4. A synchronous call that blocks a thread.
5. A slow external call with no timeout.

The C# code is almost never the cause.

## Build

1. Add Redis to `docker compose`.
2. Add `HybridCache` to the event read path.
3. Remove the cache entry when an event changes. Prove it with a test.
4. Make a fake payment service. Give it a 3-second delay and a 30% failure rate.
5. Call it with a typed `HttpClient` and the standard resilience handler.
6. Turn the fake service off. Watch the circuit breaker open in the logs.
7. Add per-user rate limiting to the booking endpoint. Test it with 200 fast requests.
8. Run a load test with k6. Note the p95 before and after the cache.

## Common mistakes

- You cache the result of a query that includes the user identity, but the key has no user identity. One user then sees another user's data.
- You add a retry to a `POST` that is not idempotent. The customer pays two times.
- You set a very long timeout "to be safe". A slow dependency then holds all your threads.
- You add a cache before you measure. You add complexity and gain nothing.

## Check

- What problem does a circuit breaker solve that a retry does not solve?
- Your cache hit rate is 99%, but the p99 latency is bad. What is the likely cause?

---

# Day 15 — Containers, observability, and the pipeline

## Why this day

Code that runs only on your machine has no value. This day makes the system portable, visible, and repeatable.

## Concepts

### The Dockerfile

Use a multi-stage build. The final image has the runtime only, not the SDK.

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy the project files first. This layer is cached if no package changes.
COPY Directory.Build.props Directory.Packages.props ./
COPY src/Api/EventHub.Api/*.csproj src/Api/EventHub.Api/
COPY src/Modules/Events/*/*.csproj src/Modules/Events/
RUN dotnet restore src/Api/EventHub.Api/EventHub.Api.csproj

# Then copy the source.
COPY . .
RUN dotnet publish src/Api/EventHub.Api/EventHub.Api.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "EventHub.Api.dll"]
```

Key points:
- Copy the `.csproj` files first, then restore, then copy the rest. Docker caches the restore layer. A code change does not repeat the package download.
- Use the `aspnet` image for the final stage, not the `sdk` image. It is much smaller.
- `USER $APP_UID` runs as a non-root user. The .NET images define this variable.
- Add a `.dockerignore` file. Exclude `bin`, `obj`, `.git`, and `.vs`.

### No Dockerfile

.NET can build a container with no Dockerfile:

```bash
dotnet publish -t:PublishContainer -p:ContainerRepository=eventhub-api
```

This is good for a simple service. Use a Dockerfile when you need control.

### Compose the full stack

```yaml
services:
  api:
    build:
      context: .
      dockerfile: build/api.Dockerfile
    ports: ["8080:8080"]
    environment:
      ASPNETCORE_ENVIRONMENT: Development
      ConnectionStrings__Default: Host=postgres;Database=eventhub;Username=eventhub;Password=local_dev_only
      ConnectionStrings__Redis: redis:6379
    depends_on:
      postgres:
        condition: service_healthy
      redis:
        condition: service_started

  postgres:
    image: postgres:17-alpine
    environment:
      POSTGRES_USER: eventhub
      POSTGRES_PASSWORD: local_dev_only
      POSTGRES_DB: eventhub
    ports: ["5432:5432"]
    volumes: ["pgdata:/var/lib/postgresql/data"]
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U eventhub"]
      interval: 5s
      retries: 10

  redis:
    image: redis:7-alpine
    ports: ["6379:6379"]

volumes:
  pgdata:
```

Note `depends_on` with `condition: service_healthy`. Without the health check, the API starts before PostgreSQL is ready, and it fails.

Inside the network, the host name is the service name. Use `Host=postgres`, not `localhost`.

### Health checks

```csharp
builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "postgres", tags: ["ready"])
    .AddRedis(redisConnectionString, name: "redis", tags: ["ready"]);

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false      // no checks. Is the process alive?
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});
```

- **Liveness** — is the process running? If it fails, restart the container.
- **Readiness** — can the process serve traffic? If it fails, stop sending requests, but do not restart.

The difference matters. If your readiness check tests the database, and the database is down for 30 seconds, a liveness check with the same test will restart all your containers. That makes the problem worse.

### Observability: the three signals

| Signal | Question | Tool |
|---|---|---|
| Logs | What happened? | Serilog, Seq, Loki |
| Metrics | How much and how fast? | Prometheus, Grafana |
| Traces | Where did the time go? | Jaeger, Zipkin, Aspire dashboard |

OpenTelemetry is the standard for all three. It is vendor neutral. Set it up one time. Change the backend later with no code change.

```csharp
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("eventhub-api"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddNpgsql()
        .AddOtlpExporter())
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddOtlpExporter());

builder.Logging.AddOpenTelemetry(o =>
{
    o.IncludeScopes = true;
    o.AddOtlpExporter();
});
```

### Custom metrics

```csharp
public sealed class BookingMetrics
{
    private readonly Counter<long> _bookingsCreated;
    private readonly Histogram<double> _bookingDuration;

    public BookingMetrics(IMeterFactory factory)
    {
        var meter = factory.Create("EventHub.Bookings");
        _bookingsCreated = meter.CreateCounter<long>("eventhub.bookings.created");
        _bookingDuration = meter.CreateHistogram<double>("eventhub.bookings.duration", "ms");
    }

    public void BookingCreated(string eventType) =>
        _bookingsCreated.Add(1, new KeyValuePair<string, object?>("event.type", eventType));
}
```

Register it as a singleton. Inject it in the handler.

### Custom traces

```csharp
private static readonly ActivitySource Source = new("EventHub.Bookings");

using var activity = Source.StartActivity("ConfirmBooking");
activity?.SetTag("booking.id", bookingId);
activity?.SetTag("booking.seats", seats);
```

A trace shows the full path of one request across all the parts. This is the most useful signal in a distributed system.

### .NET Aspire

Aspire orchestrates the local development stack. It replaces much of your compose file, and it gives a dashboard with logs, metrics, and traces with no setup.

```csharp
// in the AppHost project
var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .AddDatabase("eventhub");

var redis = builder.AddRedis("cache");

builder.AddProject<Projects.EventHub_Api>("api")
    .WithReference(postgres)
    .WithReference(redis)
    .WaitFor(postgres);

builder.Build().Run();
```

Run `dotnet run` in the AppHost project. Aspire starts the containers, injects the connection strings, and opens the dashboard.

Aspire is for development and for deployment generation. It is not a run-time framework. Your service still works without it.

I recommend Aspire for Stage 3. In Stage 2, learn `docker compose` first, so that you understand what Aspire does for you.

### CI with GitHub Actions

`.github/workflows/ci.yml`:

```yaml
name: CI

on:
  push:
    branches: [main]
  pull_request:

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'

      - name: Restore
        run: dotnet restore

      - name: Build
        run: dotnet build --no-restore -c Release

      - name: Test
        run: dotnet test --no-build -c Release --logger trx --collect:"XPlat Code Coverage"

      - name: Publish test results
        if: always()
        uses: dorny/test-reporter@v1
        with:
          name: tests
          path: '**/*.trx'
          reporter: dotnet-trx
```

Testcontainers works in GitHub Actions with no extra setup. Docker is available on the `ubuntu-latest` runner.

Add a package cache to make the build faster:

```yaml
      - uses: actions/cache@v4
        with:
          path: ~/.nuget/packages
          key: nuget-${{ hashFiles('**/Directory.Packages.props') }}
```

## Build

1. Write the `Dockerfile` and the `.dockerignore` file.
2. Put the API, PostgreSQL, and Redis in `docker compose`. Start the full stack with one command.
3. Add liveness and readiness health checks with the correct difference.
4. Add OpenTelemetry with traces and metrics.
5. Add Jaeger or the Aspire dashboard to compose. Look at one trace from end to end.
6. Add two custom metrics and one custom trace span.
7. Write the CI workflow. Confirm that the integration tests run in GitHub Actions.
8. Make the same stack with .NET Aspire. Compare the effort.

## Common mistakes

- You copy all the source before the restore. Then every code change repeats the package download. The build is slow.
- You run the container as root. This is a security risk. Use `USER $APP_UID`.
- Your liveness check tests the database. Then a short database problem restarts all your containers.
- You put a secret in the compose file and commit it. Use a `.env` file, and add it to `.gitignore`.
- You log at `Debug` level in production. The cost is high, and the useful logs are hidden.

## Check

- What is the difference between a liveness probe and a readiness probe? Give an example of a failure for each.
- Why do you copy the `.csproj` files before the source in a Dockerfile?

---

# The Stage 2 review

At the end of Day 15, you must have this:

- Three modules with real boundaries, enforced by architecture tests.
- One database, with one schema and one migration history for each module.
- Modules that talk through public contracts and integration events only.
- A transactional outbox with at-least-once delivery, and idempotent handlers.
- A `HybridCache` cache with correct invalidation.
- Resilient HTTP calls with retry, timeout, and a circuit breaker.
- The full stack in `docker compose`, started with one command.
- Traces, metrics, and structured logs through OpenTelemetry.
- A CI pipeline that builds and tests each push.

**Stop and think here.** This system can serve a real business. It handles load. It is observable. It is testable. Most companies do not need more than this.

Stage 3 is not an improvement. It is a trade. You give up simplicity and get independent deployment and independent scaling. Learn it, so that you know when the trade is correct.
