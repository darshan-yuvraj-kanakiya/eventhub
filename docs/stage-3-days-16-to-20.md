# Stage 3 — Microservices in a monorepo (Day 16 to Day 20)

## Read this before Day 16

Microservices are not a better architecture. They are a different set of trade-offs.

**What you get:**
- Each team deploys without the other teams.
- Each service scales alone.
- One service can use a different language or database.
- One failure does not stop the full process.

**What you pay:**
- Every method call becomes a network call. It can fail, and it is slow.
- You cannot use one transaction. You must use eventual consistency.
- You cannot join two tables. You must copy data or call another service.
- Debugging needs distributed tracing.
- Local development needs 6 containers, not 1.
- A test across services is hard and slow.

Most teams that adopt microservices do it too early. They get all the cost and none of the benefit. This is called a distributed monolith. It is the worst of both.

**Learn this stage for these reasons:**
1. You will work on such a system. You must understand it.
2. Interviewers ask about it.
3. When you feel the pain, you understand why Stage 2 is the correct default.

Your Stage 2 work makes this stage easy. The modules already have boundaries. The data is already separate. The events already exist. You only change the transport.

---

# Day 16 — The monorepo structure

## Why this day

Many services need a repository strategy. A monorepo keeps one version of the truth. But a bad monorepo has slow builds and confused ownership.

## Concepts

### Monorepo against polyrepo

| | Monorepo | Polyrepo |
|---|---|---|
| Change across services | One pull request | Many pull requests, in order |
| Shared code | A project reference | A NuGet package with a version |
| Build time | Long, unless you filter | Short |
| Version consistency | Automatic | Manual work |
| Access control | Hard for one folder | Easy for one repository |
| Best for | One team, or a few teams | Many independent teams |

**Note the difference again.** A monorepo is a source control choice. Microservices are a deployment choice. They are independent. You can have:
- A monolith in a monorepo (normal)
- A monolith in a polyrepo (rare)
- Microservices in a monorepo (common, and good for a small company)
- Microservices in a polyrepo (common in a large company)

Start with a monorepo. Move a service out later if a team needs it.

### The structure

```
eventhub/
├─ .github/
│  └─ workflows/
│     ├─ ci-events.yml           ← runs only for a change in the Events folder
│     ├─ ci-bookings.yml
│     └─ ci-shared.yml           ← runs everything for a change in Shared
├─ .editorconfig
├─ .gitignore
├─ Directory.Build.props          ← settings for all projects
├─ Directory.Packages.props       ← one version for each package
├─ nuget.config
├─ global.json                    ← lock the SDK version
├─ EventHub.sln                   ← everything
├─ EventHub.Events.slnf           ← a filter: the Events service only
├─ build/
│  ├─ api.Dockerfile
│  └─ scripts/
├─ deploy/
│  ├─ docker-compose.yml
│  ├─ docker-compose.override.yml
│  └─ k8s/
├─ docs/
│  ├─ architecture.md
│  └─ adr/
│     ├─ 0001-use-modular-monolith-first.md
│     └─ 0002-postgres-per-service.md
├─ src/
│  ├─ Gateway/
│  │  └─ EventHub.Gateway/
│  ├─ Services/
│  │  ├─ Events/
│  │  │  ├─ EventHub.Events.Api/
│  │  │  ├─ EventHub.Events.Application/
│  │  │  ├─ EventHub.Events.Domain/
│  │  │  └─ EventHub.Events.Infrastructure/
│  │  ├─ Bookings/
│  │  └─ Notifications/
│  └─ Shared/
│     ├─ EventHub.Shared.Contracts/     ← the message definitions ONLY
│     └─ EventHub.Shared.Kernel/        ← Result, Error, base types
└─ tests/
   ├─ Services/Events/
   ├─ Services/Bookings/
   └─ EventHub.SystemTests/
```

### Solution filters

A solution with 40 projects opens slowly. A filter loads a subset.

Make `EventHub.Events.slnf`:

```json
{
  "solution": {
    "path": "EventHub.sln",
    "projects": [
      "src\\Services\\Events\\EventHub.Events.Api\\EventHub.Events.Api.csproj",
      "src\\Services\\Events\\EventHub.Events.Domain\\EventHub.Events.Domain.csproj",
      "src\\Shared\\EventHub.Shared.Contracts\\EventHub.Shared.Contracts.csproj",
      "tests\\Services\\Events\\EventHub.Events.UnitTests\\EventHub.Events.UnitTests.csproj"
    ]
  }
}
```

Open this file in your IDE. You see the Events service only.

### The shared code problem

This is the most common failure in a monorepo with microservices. A developer sees duplicate code and moves it to Shared. After one year, Shared holds everything. Now no service can deploy alone. You have a distributed monolith.

**What you may put in Shared:**
- Message and event contracts (records with primitive fields).
- Technical base types: `Result`, `Error`, `PagedList`.
- Cross-cutting helpers: a correlation identifier, telemetry setup, authentication setup.

**What you must never put in Shared:**
- A business entity. `Event` belongs to the Events service only.
- A `DbContext` or any data access.
- A business rule.
- A DTO for one service's API.
- A "common utilities" project with no clear purpose.

**The test:** if a change to a shared project forces you to deploy all services at the same time, that project must not be shared.

Duplication between services is acceptable. It is better than a wrong coupling. The Bookings service can have its own small `EventInfo` record. It is not the same as the Events service `Event` entity, and it must not be.

### Directory.Build.props for a monorepo

```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
    <LangVersion>latest</LangVersion>
    <InvariantGlobalization>true</InvariantGlobalization>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.CodeAnalysis.NetAnalyzers" PrivateAssets="all" />
  </ItemGroup>
</Project>
```

You can also add a second `Directory.Build.props` in a subfolder. MSBuild finds the nearest one. To also use the root one, import it:

```xml
<Project>
  <Import Project="$([MSBuild]::GetPathOfFileAbove('Directory.Build.props', '$(MSBuildThisFileDirectory)../'))" />
  <PropertyGroup>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>
</Project>
```

### CI with path filters

Do not build everything for every change.

```yaml
name: CI Events

on:
  pull_request:
    paths:
      - 'src/Services/Events/**'
      - 'src/Shared/**'
      - 'Directory.*.props'
      - '.github/workflows/ci-events.yml'

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      - run: dotnet test EventHub.Events.slnf -c Release
```

Note that a change in `Shared` triggers all the service workflows. This is correct. It also shows you the cost of shared code.

### Decision records

Write down why you chose something. Your future self will ask.

`docs/adr/0002-database-per-service.md`:

```markdown
# 2. One database for each service

Date: 2026-09-20
Status: Accepted

## Context
The Events service and the Bookings service now run as separate processes.
They shared one PostgreSQL database with two schemas.

## Decision
Each service gets its own PostgreSQL database with its own credentials.

## Consequences
Good: A service can change its schema without any coordination.
Good: A load problem in one service does not affect the other.
Bad: We cannot join across services. Bookings must keep a copy of the event title.
Bad: We run two database containers in local development.
```

Keep it short. One page. Never delete one; mark it as superseded.

## Build

1. Make the folder structure above. Move the module projects into `src/Services/`.
2. Rename the projects: `EventHub.Modules.Events.Domain` becomes `EventHub.Events.Domain`.
3. Make `EventHub.Shared.Contracts` and `EventHub.Shared.Kernel`. Move the correct types only.
4. Read your Common projects from Stage 2. Move each type to Shared, or copy it into each service. Give a reason for each choice.
5. Make a solution filter for each service.
6. Make the CI workflow with path filters. Test it: change one file in Events, and confirm that only the Events job runs.
7. Write two decision records.

## Common mistakes

- You move all the Common code to Shared with no thought. Read each type. Ask the deployment question.
- You give the same version number to all the services. They do not need one. Version each service alone.
- You make one giant CI job. The feedback becomes slow, and developers stop reading it.

## Check

- Your teammate wants to move the `Booking` entity to Shared, "because Notifications needs the seat count". What do you say?
- What is the difference between a monorepo and a monolith?

---

# Day 17 — Service separation and synchronous calls

## Why this day

Today you break the process boundary. This is where the real problems start. Feel them.

## Concepts

### One database for each service

This is the rule that defines a microservice. Without it, you have a distributed monolith.

Why is it so important? Because a shared database is a shared contract. If the Bookings service reads the `events` table, then the Events team cannot rename a column. They must ask everybody. Deployment is now coordinated. You lost the only real benefit.

```yaml
services:
  events-db:
    image: postgres:17-alpine
    environment:
      POSTGRES_DB: events
      POSTGRES_USER: events_user
      POSTGRES_PASSWORD: local_dev_only
    ports: ["5433:5432"]

  bookings-db:
    image: postgres:17-alpine
    environment:
      POSTGRES_DB: bookings
      POSTGRES_USER: bookings_user
      POSTGRES_PASSWORD: local_dev_only
    ports: ["5434:5432"]
```

In production, you can put them in the same PostgreSQL server as separate databases with separate users. What matters is that no service can read another service's tables.

### Data duplication is correct

The booking list screen shows the event title. The Bookings service does not own the title. What do you do?

**Option A: call the Events service for each request.** Simple, but slow, and Bookings fails when Events fails.

**Option B: keep a copy.** Bookings has a small `event_snapshots` table with the identifier, the title, and the start time. It updates the copy when it receives an `EventUpdated` event.

Option B is normally correct. It feels wrong to a developer with a database background, because it breaks normalization. But normalization is a rule for one database. Across services, a copy is the correct answer.

The copy is not the truth. Events owns the truth. The copy is a read model, and it can be a few seconds old. Ask the business if that is acceptable. It almost always is.

### Synchronous communication: REST or gRPC

Use it only when you need the answer now, and when a stale answer is not acceptable.

**REST with a typed client:**

```csharp
public interface IEventsApiClient
{
    Task<EventSummary?> GetEventAsync(Guid id, CancellationToken ct);
}

internal sealed class EventsApiClient(HttpClient httpClient) : IEventsApiClient
{
    public async Task<EventSummary?> GetEventAsync(Guid id, CancellationToken ct)
    {
        var response = await httpClient.GetAsync($"/api/v1/events/{id}", ct);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<EventSummary>(ct);
    }
}

builder.Services.AddHttpClient<IEventsApiClient, EventsApiClient>(client =>
    {
        client.BaseAddress = new Uri("http://events-api");
        client.Timeout = TimeSpan.FromSeconds(5);
    })
    .AddStandardResilienceHandler();
```

**gRPC** is better for internal service-to-service calls. It uses HTTP/2 and Protobuf. It is faster and smaller than JSON. The contract is a `.proto` file, and it generates both the client and the server.

```protobuf
syntax = "proto3";
option csharp_namespace = "EventHub.Events.Grpc";

service EventsService {
  rpc GetEvent (GetEventRequest) returns (EventReply);
  rpc ReserveSeats (ReserveSeatsRequest) returns (ReserveSeatsReply);
}

message GetEventRequest { string id = 1; }

message EventReply {
  string id = 1;
  string title = 2;
  int32 available_seats = 3;
}
```

Add the file to both projects:

```xml
<!-- server -->
<Protobuf Include="Protos/events.proto" GrpcServices="Server" />
<!-- client -->
<Protobuf Include="Protos/events.proto" GrpcServices="Client" />
```

The server:

```csharp
public sealed class EventsGrpcService(IEventReader reader) : EventsService.EventsServiceBase
{
    public override async Task<EventReply> GetEvent(GetEventRequest request, ServerCallContext context)
    {
        var item = await reader.GetAsync(Guid.Parse(request.Id), context.CancellationToken);

        if (item is null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "The event does not exist."));
        }

        return new EventReply { Id = item.Id.ToString(), Title = item.Title, AvailableSeats = item.AvailableSeats };
    }
}

app.MapGrpcService<EventsGrpcService>();
```

The client:

```csharp
builder.Services.AddGrpcClient<EventsService.EventsServiceClient>(o =>
{
    o.Address = new Uri("http://events-api");
}).AddStandardResilienceHandler();
```

**Which one?** Use gRPC between services. Use REST for the public API, because browsers and external clients need it.

### Every remote call can fail

This is the largest change in your thinking. In a monolith:

```csharp
var evt = await eventReader.GetAsync(id, ct);   // it works, or there is a bug
```

Between services:

```csharp
var evt = await eventsClient.GetEventAsync(id, ct);
// It can: return the data, return null, time out, refuse the connection,
// return a 500 error, return after 30 seconds, or return stale data.
```

For each call, answer these questions:
1. What is the timeout? (Always set one. Never use infinity.)
2. Do I retry? (Only if the call is idempotent.)
3. What do I do if it fails? Fail my own request? Use a default? Use a cached value?
4. Does my service work when this dependency is down?

If the answer to 4 is "no" for many dependencies, you built a distributed monolith. The availability of your service is the product of all the dependencies. Three services at 99.9% give you 99.7%.

### The fallback

```csharp
public async Task<EventSummary?> GetEventAsync(Guid id, CancellationToken ct)
{
    try
    {
        return await _client.GetEventAsync(id, ct);
    }
    catch (RpcException ex) when (ex.StatusCode is StatusCode.Unavailable or StatusCode.DeadlineExceeded)
    {
        logger.LogWarning(ex, "The Events service is not available. Use the local snapshot.");
        return await _snapshots.GetAsync(id, ct);   // maybe old data, but the service works
    }
}
```

## Build

1. Split the modular monolith into two hosts: `EventHub.Events.Api` and `EventHub.Bookings.Api`.
2. Give each its own database in `docker compose`. Move the migrations.
3. Add a gRPC service to Events with `GetEvent` and `ReserveSeats`.
4. Call it from Bookings with a gRPC client and the standard resilience handler.
5. Add an `event_snapshots` table to the Bookings database. Fill it by hand for now. Day 18 fills it with events.
6. Add a fallback to the snapshot when the gRPC call fails.
7. **Do this test.** Stop the Events container. Try to create a booking. Try to read the booking list. Note what works and what fails. Write down the result. This is the true cost of the split.

## Common mistakes

- You keep one database "for now". Then you never split it, and you have all the cost with no benefit.
- You make a chain: Gateway → Bookings → Events → Users. Each hop adds latency and a failure point. Keep the chains short.
- You return the internal domain entity over gRPC. The contract must be separate from the domain model.
- You have no timeout. One slow service then holds all the threads in the calling service.

## Check

- The Events service is down. Which parts of EventHub still work? Which parts must still work?
- Why is data duplication between services acceptable?

---

# Day 18 — Message-based integration

## Why this day

Synchronous calls couple your services. A message broker removes the coupling. This is how real systems stay available.

## Concepts

### Why messages are better

With a synchronous call, the caller waits, and the caller fails if the receiver fails.

With a message, the caller writes to the broker and continues. The receiver reads when it is ready. If the receiver is down, the message waits in the queue.

Rule: **use messages by default. Use a synchronous call only when you need the answer to continue.**

### RabbitMQ and MassTransit

RabbitMQ is the broker. MassTransit is the .NET library over it. MassTransit handles the serialization, the retries, the topology, and the outbox.

```yaml
rabbitmq:
  image: rabbitmq:3-management-alpine
  ports: ["5672:5672", "15672:15672"]
  environment:
    RABBITMQ_DEFAULT_USER: eventhub
    RABBITMQ_DEFAULT_PASS: local_dev_only
```

The management UI is at port 15672. Use it to see the queues and the messages.

Note: MassTransit version 9 moved to a commercial licence for new versions. Version 8 stays free and open source. Check the current terms before you use it in a product. The free alternatives are Wolverine, NServiceBus (also paid), or the raw RabbitMQ client. The patterns are the same in all of them.

### Publish against send

- **Publish** — "this happened". Zero or more subscribers receive a copy. Use a past-tense event name: `BookingConfirmed`.
- **Send** — "do this". Exactly one consumer receives it. Use a command name: `SendEmail`.

Publish for an event. Send for a command. This is the most common confusion.

### Setup

```csharp
builder.Services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();
    x.AddConsumers(typeof(BookingConfirmedConsumer).Assembly);

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(configuration["RabbitMq:Host"], h =>
        {
            h.Username(configuration["RabbitMq:Username"]!);
            h.Password(configuration["RabbitMq:Password"]!);
        });

        cfg.UseMessageRetry(r => r.Exponential(
            retryLimit: 5,
            minInterval: TimeSpan.FromSeconds(1),
            maxInterval: TimeSpan.FromMinutes(1),
            intervalDelta: TimeSpan.FromSeconds(2)));

        cfg.ConfigureEndpoints(context);
    });
});
```

### The contract

Put it in `EventHub.Shared.Contracts`. Both sides reference it.

```csharp
namespace EventHub.Shared.Contracts.Bookings;

public sealed record BookingConfirmed
{
    public required Guid BookingId { get; init; }
    public required Guid EventId { get; init; }
    public required Guid UserId { get; init; }
    public required int Seats { get; init; }
    public required DateTimeOffset OccurredAtUtc { get; init; }
}
```

Keep it flat. Use primitive types only. Do not put an entity, an interface, or a method in a contract.

### Publish

```csharp
public sealed class ConfirmBookingHandler(
    IBookingRepository repository,
    IPublishEndpoint publishEndpoint,
    IUnitOfWork unitOfWork)
{
    public async Task<Result> HandleAsync(ConfirmBookingCommand command, CancellationToken ct)
    {
        var booking = await repository.GetAsync(command.BookingId, ct);
        if (booking is null) { return Result.Failure(BookingErrors.NotFound); }

        booking.Confirm();

        await publishEndpoint.Publish(new BookingConfirmed
        {
            BookingId = booking.Id,
            EventId = booking.EventId,
            UserId = booking.UserId,
            Seats = booking.Seats,
            OccurredAtUtc = DateTimeOffset.UtcNow
        }, ct);

        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}
```

### Consume

```csharp
public sealed class SendBookingEmailConsumer(
    IEmailSender emailSender,
    ILogger<SendBookingEmailConsumer> logger)
    : IConsumer<BookingConfirmed>
{
    public async Task Consume(ConsumeContext<BookingConfirmed> context)
    {
        var message = context.Message;

        logger.LogInformation("Send an email for booking {BookingId}", message.BookingId);

        await emailSender.SendAsync(
            message.UserId,
            $"Your booking for {message.Seats} seats is confirmed.",
            context.CancellationToken);
    }
}
```

MassTransit finds the consumer, makes the queue, and binds the exchange. You write no infrastructure code.

### The outbox again

`IPublishEndpoint.Publish` sends the message immediately. If `SaveChangesAsync` then fails, you sent a message for a booking that does not exist.

MassTransit has a built-in outbox that uses your `DbContext`:

```csharp
x.AddEntityFrameworkOutbox<BookingsDbContext>(o =>
{
    o.UsePostgres();
    o.UseBusOutbox();
    o.QueryDelay = TimeSpan.FromSeconds(1);
});
```

Now `Publish` writes to a table inside your transaction. A background delivery service sends it after the commit. This is the Day 13 pattern, but you do not write it yourself.

Add the outbox migration:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.AddInboxStateEntity();
    modelBuilder.AddOutboxMessageEntity();
    modelBuilder.AddOutboxStateEntity();
}
```

### Errors, retries, and the dead-letter queue

MassTransit gives you three levels:

1. **Retry** — try again in the same process, after a short delay. Use it for a temporary fault.
2. **Redelivery** — put the message back in the queue with a longer delay (minutes or hours). Use it when a dependency is down.
3. **Error queue** — after all the retries fail, the message goes to `queue-name_error`. It waits there for a human.

```csharp
cfg.UseMessageRetry(r => r.Immediate(3));
cfg.UseDelayedRedelivery(r => r.Intervals(
    TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(15), TimeSpan.FromMinutes(30)));
```

Watch the error queues. An empty error queue means the system is healthy. A growing error queue is an alert.

**Do not retry a permanent error.** If the message data is invalid, a retry will never work. Throw a specific exception and let it go to the error queue immediately.

### Idempotency, again

At-least-once delivery means a consumer will sometimes see the same message two times. Reasons: a network problem after the work but before the acknowledgement, a retry, or a broker redelivery.

MassTransit gives an inbox with `AddEntityFrameworkOutbox`. It stores the message identifier and skips a duplicate.

You must still think about it. Ask for each consumer: "if this runs two times, what happens?"

- Send an email → the user gets two emails. Bad. Use the inbox.
- Set a status to Confirmed → no harm. It is naturally idempotent.
- Add 3 to a seat count → 6 seats. Very bad. Use the inbox, or compute the total.

### Eventual consistency

The booking is confirmed. The seat count in the Events service updates 200 ms later. In that gap, the two services disagree.

This is not a bug. It is the design. You must:
- Tell the business, and get their agreement.
- Design the UI for it. Show "Processing..." instead of a wrong number.
- Never show a number that the user will see change with no action.

If a case truly needs immediate consistency, the two parts belong in the same service. Consistency needs are a boundary signal.

### Message versioning

Your contract is a public API. Other teams depend on it.

**Safe changes:**
- Add an optional field with a default value.
- Add a new message type.

**Unsafe changes:**
- Remove a field.
- Rename a field.
- Change a type (`int` to `string`).
- Change the meaning of a field.

For a real breaking change, make `BookingConfirmedV2`. Publish both for a period. Move the consumers. Then stop V1.

## Build

1. Add RabbitMQ to `docker compose`.
2. Add MassTransit to Bookings, Events, and Notifications.
3. Move `BookingConfirmed` to `Shared.Contracts`.
4. Publish it from Bookings with the EF Core outbox.
5. Consume it in Notifications. Send a fake email.
6. Consume it in Events. Update the seat count.
7. Publish `EventUpdated` from Events. Consume it in Bookings to update the snapshot table.
8. **Test 1:** stop Notifications. Make three bookings. Start Notifications. Confirm that all three emails are sent.
9. **Test 2:** make the email sender throw. Watch the retries. Watch the message go to the error queue. Look at it in the RabbitMQ UI.
10. **Test 3:** send the same message two times by hand. Confirm that only one email is sent.

## Common mistakes

- You publish before `SaveChanges` with no outbox. You send events for data that does not exist.
- You put an interface or a base class in a message contract. Use a flat record.
- You use `Publish` for a command. Then two instances of a consumer both do the work.
- You do not watch the error queue. Messages fail silently for weeks.
- You expect immediate consistency. Then your tests fail randomly. Test with a wait or a poll.

## Check

- Explain at-least-once delivery, and what it forces you to do.
- When is a synchronous call better than a message?

---

# Day 19 — The gateway, security, and sagas

## Why this day

The client must not know about five services. And a business process across services needs a plan for failure.

## Concepts

### The API gateway

A gateway is one entry point. It gives you:
- One address and one certificate for the client.
- Authentication in one place.
- Rate limiting across services.
- Request routing and load balancing.
- A stable public path when the services change.

YARP (Yet Another Reverse Proxy) is Microsoft's library. It is a normal ASP.NET Core application.

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.MapReverseProxy();

app.Run();
```

The configuration:

```json
{
  "ReverseProxy": {
    "Routes": {
      "events-route": {
        "ClusterId": "events-cluster",
        "AuthorizationPolicy": "authenticated",
        "Match": { "Path": "/api/events/{**catch-all}" },
        "Transforms": [ { "PathPattern": "/api/v1/events/{**catch-all}" } ]
      },
      "bookings-route": {
        "ClusterId": "bookings-cluster",
        "AuthorizationPolicy": "authenticated",
        "Match": { "Path": "/api/bookings/{**catch-all}" }
      }
    },
    "Clusters": {
      "events-cluster": {
        "LoadBalancingPolicy": "RoundRobin",
        "Destinations": {
          "d1": { "Address": "http://events-api:8080/" }
        },
        "HealthCheck": {
          "Active": { "Enabled": true, "Path": "/health/ready", "Interval": "00:00:10" }
        }
      },
      "bookings-cluster": {
        "Destinations": { "d1": { "Address": "http://bookings-api:8080/" } }
      }
    }
  }
}
```

**Keep the gateway thin.** A gateway with business logic becomes the new monolith, and every team must change it. It routes, it authenticates, and it limits. Nothing more.

### Security across services

**At the gateway:** validate the JWT. Reject a bad token before it reaches a service.

**At the service:** validate the token again. Never trust the network. This is defence in depth. An attacker inside your network must not be able to call a service directly with no token.

Send the identity forward. The simplest method is to pass the original token, and let each service validate it.

For service-to-service calls with no user (a background job), use the client credentials flow. The service gets its own token.

### Correlation across services

One user action now touches four services. You need one identifier to follow it.

```csharp
// in the gateway, add a header if it does not exist
app.Use(async (context, next) =>
{
    if (!context.Request.Headers.ContainsKey("X-Correlation-Id"))
    {
        context.Request.Headers["X-Correlation-Id"] = Guid.CreateVersion7().ToString();
    }
    await next();
});
```

OpenTelemetry does most of this for you. It propagates the W3C `traceparent` header through HTTP, gRPC, and MassTransit. Your traces then join automatically.

Add the trace identifier to each log line:

```csharp
logger.LogInformation("Booking {BookingId} confirmed. TraceId={TraceId}",
    bookingId, Activity.Current?.TraceId);
```

Now you can take a trace identifier from an error report and see every log line from every service for that one request.

### Service discovery

How does Bookings find Events?

- **Docker Compose:** the service name is the host name. `http://events-api:8080`. Compose gives the DNS.
- **Kubernetes:** the service name gives DNS too. `http://events-api.default.svc.cluster.local`.
- **.NET service discovery:** the `Microsoft.Extensions.ServiceDiscovery` package lets you write `http://events-api` and resolve it from configuration or from the platform.

```csharp
builder.Services.AddServiceDiscovery();
builder.Services.ConfigureHttpClientDefaults(http =>
{
    http.AddServiceDiscovery();
    http.AddStandardResilienceHandler();
});
```

Never write an IP address in the configuration.

### The saga

A booking is: reserve the seats, take the payment, confirm the booking. Three services. There is no distributed transaction. What happens if the payment fails after the seats are reserved?

You need a **saga**: a sequence of local transactions, with a compensating action for each step.

| Step | Service | Compensation |
|---|---|---|
| 1. Reserve the seats | Events | Release the seats |
| 2. Take the payment | Payments | Refund the payment |
| 3. Confirm the booking | Bookings | Cancel the booking |

If step 2 fails, run the compensation for step 1. The system returns to a correct state.

**Note:** a compensation is not a rollback. The event happened. A refund is a new transaction, and the customer sees both lines. Design for this. Tell the business.

### Two styles

**Choreography** — no coordinator. Each service listens and reacts.

```
Bookings publishes BookingRequested
  → Events consumes it, reserves the seats, publishes SeatsReserved
    → Payments consumes it, takes the payment, publishes PaymentCompleted
      → Bookings consumes it and confirms
```

Good: no single point of control. Services stay independent.
Bad: nobody can see the full process. Debugging is hard. A cycle is easy to create by mistake.

**Orchestration** — one coordinator, a state machine.

```csharp
public sealed class BookingSaga : MassTransitStateMachine<BookingSagaState>
{
    public State AwaitingSeats { get; private set; } = null!;
    public State AwaitingPayment { get; private set; } = null!;

    public Event<BookingRequested> BookingRequested { get; private set; } = null!;
    public Event<SeatsReserved> SeatsReserved { get; private set; } = null!;
    public Event<SeatsUnavailable> SeatsUnavailable { get; private set; } = null!;
    public Event<PaymentCompleted> PaymentCompleted { get; private set; } = null!;
    public Event<PaymentFailed> PaymentFailed { get; private set; } = null!;

    public BookingSaga()
    {
        InstanceState(x => x.CurrentState);

        Initially(
            When(BookingRequested)
                .Then(ctx =>
                {
                    ctx.Saga.BookingId = ctx.Message.BookingId;
                    ctx.Saga.EventId = ctx.Message.EventId;
                    ctx.Saga.Seats = ctx.Message.Seats;
                })
                .Publish(ctx => new ReserveSeats
                {
                    CorrelationId = ctx.Saga.CorrelationId,
                    EventId = ctx.Saga.EventId,
                    Seats = ctx.Saga.Seats
                })
                .TransitionTo(AwaitingSeats));

        During(AwaitingSeats,
            When(SeatsReserved)
                .Publish(ctx => new TakePayment { /* ... */ })
                .TransitionTo(AwaitingPayment),
            When(SeatsUnavailable)
                .Publish(ctx => new RejectBooking { BookingId = ctx.Saga.BookingId })
                .Finalize());

        During(AwaitingPayment,
            When(PaymentCompleted)
                .Publish(ctx => new ConfirmBooking { BookingId = ctx.Saga.BookingId })
                .Finalize(),
            When(PaymentFailed)
                .Publish(ctx => new ReleaseSeats { /* compensation */ })
                .Publish(ctx => new RejectBooking { BookingId = ctx.Saga.BookingId })
                .Finalize());

        SetCompletedWhenFinalized();
    }
}
```

The saga state is in a database. If the process stops, the saga continues after the restart.

Add a timeout for each step. If the payment service never answers, the saga must not wait forever. Schedule a timeout message and compensate.

**Which style?** Use choreography for two or three steps. Use orchestration for four or more, or when the business needs to see the process state. Orchestration is easier to understand and to debug.

## Build

1. Make the `EventHub.Gateway` project with YARP.
2. Route `/api/events` and `/api/bookings` to the correct services.
3. Validate the JWT at the gateway. Keep the validation in the services too.
4. Add active health checks to the clusters. Stop one service, and watch YARP remove it.
5. Add service discovery. Remove the hard-coded addresses.
6. Make a fake Payments service that fails 30% of the time.
7. Write the booking saga with MassTransit. Include the compensations.
8. Add a timeout for the payment step.
9. **Test:** make 20 bookings. Confirm that each one ends as confirmed or as rejected with the seats released. No booking must stay in the middle state.
10. Open the trace view. Follow one booking across the gateway, Bookings, Events, and Payments.

## Common mistakes

- You put business logic in the gateway. Every team must then change it, and it becomes the bottleneck.
- Your saga has no timeout. A lost message leaves the process stuck forever.
- You compensate with a delete. Do not hide the history. Add a compensating record.
- You forget that a compensation can also fail. Log it, alert, and let a human act.

## Check

- Compare choreography and orchestration. When do you use each?
- Why must a service validate the token again, when the gateway already did it?

---

# Day 20 — Delivery, operations, and the review

## Why this day

The last day makes the system deployable, and it makes you review what you learned.

## Concepts

### Database migrations in production

Never do this in production:

```csharp
await context.Database.MigrateAsync();   // at application start
```

Why not:
- Many instances start at the same time and fight over the lock.
- A slow migration blocks the start, and the health check fails, and the platform restarts the pod, and the migration restarts.
- The application user needs schema permission. This is a security risk.
- A failed migration leaves the application in an unknown state.

Do this instead:
1. Generate an idempotent SQL script: `dotnet ef migrations script --idempotent --output migration.sql`.
2. Review the SQL in the pull request. A human reads it.
3. Run it as a separate CI step, or as a Kubernetes init job, before the new version deploys.
4. Use a separate database user with schema permission for the migration only.

### Backward-compatible migrations

During a deployment, version 1 and version 2 of the code run at the same time. The database must work with both.

**Never do this in one release:** rename a column.

**Do this in three releases:**
1. Add the new column. Write to both columns. Read from the old one.
2. Copy the old data to the new column. Read from the new one. Still write to both.
3. Stop writing to the old column. Drop it.

This is the expand and contract pattern. It is slow, but it means zero downtime.

### Configuration and secrets

| Environment | Method |
|---|---|
| Local | User secrets, a `.env` file |
| CI | The repository or organization secrets |
| Production | The platform: Azure Key Vault, AWS Secrets Manager, Kubernetes secrets, HashiCorp Vault |

Rules:
- Never commit a secret. Add a scanner such as `gitleaks` to CI.
- Rotate a secret if it appears in a log or in a repository. Assume it is public.
- Read the configuration at start. Fail fast with `ValidateOnStart`.
- Use one connection string per service, with its own user and least privilege.

### Deployment strategies

| Strategy | How | Risk |
|---|---|---|
| Recreate | Stop all, start all | Downtime |
| Rolling | Replace the instances one at a time | Low. The default in Kubernetes. |
| Blue-green | Two full environments. Switch the traffic. | Low. It costs two times more. |
| Canary | Send 5% of the traffic to the new version. Watch. Increase. | Lowest, but it needs good metrics. |

For all of them, both versions run at the same time. So your API and your database must be backward compatible. This is the rule that matters, not the strategy.

**Feature flags** separate the deployment from the release. Deploy the code with the feature off. Turn it on for 1% of the users. Turn it off with no deployment if there is a problem.

### Kubernetes, briefly

You do not need to be an expert. Know these five objects:

- **Pod** — one or more containers that run together. You do not make one directly.
- **Deployment** — it manages the pods. It handles the rolling update and the replica count.
- **Service** — a stable name and IP address for a group of pods. It load balances.
- **Ingress** — routes external HTTP traffic to a service.
- **ConfigMap and Secret** — configuration and secrets, given as environment variables or files.

A minimal deployment:

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: events-api
spec:
  replicas: 3
  selector:
    matchLabels: { app: events-api }
  template:
    metadata:
      labels: { app: events-api }
    spec:
      containers:
        - name: api
          image: ghcr.io/myorg/events-api:1.4.0
          ports: [{ containerPort: 8080 }]
          env:
            - name: ConnectionStrings__Default
              valueFrom:
                secretKeyRef: { name: events-secrets, key: db-connection }
          livenessProbe:
            httpGet: { path: /health/live, port: 8080 }
            initialDelaySeconds: 10
          readinessProbe:
            httpGet: { path: /health/ready, port: 8080 }
          resources:
            requests: { cpu: 100m, memory: 128Mi }
            limits: { memory: 512Mi }
```

Set a memory limit, but be careful with a CPU limit. A CPU limit causes throttling, and it makes latency worse. Set a CPU request, not a CPU limit.

Handle the shutdown correctly. Kubernetes sends SIGTERM, then waits, then kills. ASP.NET Core handles this. Set a grace period so that in-flight requests can finish:

```csharp
builder.Services.Configure<HostOptions>(o =>
    o.ShutdownTimeout = TimeSpan.FromSeconds(30));
```

### The CD pipeline

```yaml
name: CD Events

on:
  push:
    branches: [main]
    paths: ['src/Services/Events/**', 'src/Shared/**']

jobs:
  build-and-push:
    runs-on: ubuntu-latest
    permissions:
      contents: read
      packages: write
    steps:
      - uses: actions/checkout@v4

      - uses: docker/login-action@v3
        with:
          registry: ghcr.io
          username: ${{ github.actor }}
          password: ${{ secrets.GITHUB_TOKEN }}

      - uses: docker/build-push-action@v6
        with:
          context: .
          file: build/events.Dockerfile
          push: true
          tags: |
            ghcr.io/${{ github.repository }}/events-api:${{ github.sha }}
            ghcr.io/${{ github.repository }}/events-api:latest
          cache-from: type=gha
          cache-to: type=gha,mode=max
```

Tag the image with the commit hash. Never deploy `latest` to production. You must know exactly which code runs.

### What to watch in production

Four signals for each service:
1. **Latency** — p50, p95, p99. Not the average.
2. **Traffic** — requests per second.
3. **Errors** — the rate of 5xx responses.
4. **Saturation** — CPU, memory, the connection pool, the queue depth.

Set an alert on the symptom that the user feels, not on the cause. Alert on "the error rate is above 1%", not on "the CPU is at 80%". High CPU with a happy user is not a problem.

Also watch:
- The dead-letter queue depth. It must be zero.
- The outbox age. If the oldest unprocessed message is 10 minutes old, something is broken.
- The saga instances that are stuck in one state.

## Build

1. Write the CD workflow for each service. Push the images to a registry.
2. Generate an idempotent migration script. Add it as a separate pipeline step.
3. Write a Kubernetes manifest for one service, with probes and resource requests. You do not need to deploy it. Write it and understand it.
4. Add a feature flag to one new feature.
5. Add a `gitleaks` scan to CI.
6. Write `README.md`. Include: what the system does, how to run it locally with one command, the architecture diagram, and how to run the tests.
7. Write three more decision records.
8. Add a metric for the outbox age and one for the dead-letter depth.

## The review

Read the code that you wrote on Day 1. Then answer these:

- What would you write differently now?
- Which day was the most difficult? Do that topic again.
- Which pattern did you add but do not need? Remove it.
- Can a new developer start the full system with one command? Test it. Delete your Docker volumes and try.

Now answer these design questions with no help:

1. A new requirement says that a booking must send an SMS message as well as an email. Which service changes? How many deployments?
2. The Events service is slow. How do you find the cause?
3. A user says "I booked a seat but got no email". How do you investigate? Which identifier do you use?
4. Your team is 4 people and the system has 200 users a day. Was Stage 3 the correct choice? Give the honest answer.
5. You must add a Payments service that a partner company builds. Which part of your design makes this easy or hard?

If you can answer all five with concrete detail, you finished the roadmap.

---

# What to do after Day 20

**Go deeper on the parts that you use.**
- Read the ASP.NET Core source on GitHub. It is readable, and it teaches you much.
- Learn PostgreSQL well: `EXPLAIN ANALYZE`, index types, and the connection pool. This gives more value than any C# skill.
- Learn one cloud platform deeply: Azure, AWS, or Google Cloud. Choose the one that your company uses.

**Build a second project with no guide.** Choose a different domain. Do not copy EventHub. The second project is where the learning becomes yours.

**Read these books:**
- *Domain-Driven Design Distilled* by Vaughn Vernon — short and practical.
- *Designing Data-Intensive Applications* by Martin Kleppmann — the best book on distributed systems.
- *Release It!* by Michael Nygard — how systems fail in production.

**Practise the honest answer.** In an interview, when they ask "would you use microservices?", the best answer is "it depends, and usually no". Then explain the trade. That answer shows more experience than a list of patterns.
