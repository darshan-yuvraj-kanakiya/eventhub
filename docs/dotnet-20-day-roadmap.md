# 20-Day .NET Revision Roadmap

For a developer who returns to .NET after two years of React work.

Target stack: **.NET 10 (LTS)**, **C# 14**, **PostgreSQL**, **EF Core 10**, **Docker**.

.NET 10 came out in November 2025. Microsoft gives support to November 2028. Install this version. Do not start a new project on .NET 8.

---

## 1. How to use this plan

- Give 2 to 3 hours to each day.
- Each day has three parts: **Learn**, **Build**, and **Check**.
- Do the **Build** part in the same practice project each day. Do not make a new project each day.
- Do not read only. Write the code.
- Commit your work to Git each day. Use one branch for each day.
- If a day takes more time, move the next days. The order is more important than the speed.

### The practice project

Build **EventHub**, a small system for events and bookings.

The system has these functions:
- An organizer creates an event. The event has a title, a date, a place, and a number of seats.
- A user makes a booking for an event.
- The system refuses a booking if no seats stay free.
- The system sends a notification after a booking.
- A user cancels a booking.

This domain is small. But it divides into services later: Events, Bookings, and Notifications.

### The three stages

| Stage | Days | Result |
|---|---|---|
| 1. Single service | 1 to 11 | One Web API with clean layers |
| 2. Modular monolith | 12 to 15 | One process with independent modules |
| 3. Microservices in a monorepo | 16 to 20 | Three services, a gateway, and messages |

---

## Stage 1 — Modern C# and one Web API (Day 1 to Day 11)

### Day 1 — The .NET 10 platform and the project setup

**Learn**
- The .NET CLI: `dotnet new`, `dotnet build`, `dotnet run`, `dotnet watch`, `dotnet test`.
- The new project file. It is small now. It uses SDK-style format.
- Top-level statements in `Program.cs`. The `Startup.cs` file does not exist any more.
- Implicit usings and global usings.
- File-scoped namespaces.
- Nullable reference types (`<Nullable>enable</Nullable>`).
- Central Package Management with `Directory.Packages.props`.

**Build**
1. Install the .NET 10 SDK. Run `dotnet --info` to confirm the version.
2. Create the solution: `dotnet new sln -n EventHub`.
3. Create the API: `dotnet new webapi -n EventHub.Api --use-controllers`.
4. Add a `.editorconfig` file, a `.gitignore` file, and a `Directory.Build.props` file.
5. Set `TreatWarningsAsErrors` to `true` in `Directory.Build.props`.
6. Start the API with `dotnet watch`.

**Check**
- Can you explain what the host builder does?
- Why do warnings from nullable reference types help you?

---

### Day 2 — C# syntax refresh, part 1

**Learn**
- `record` and `record struct` types. Value equality. The `with` expression.
- `init` accessors and `required` members.
- Primary constructors for classes.
- Target-typed `new`.
- Collection expressions: `int[] x = [1, 2, 3];` and the spread operator `..`.
- Pattern matching: type patterns, property patterns, list patterns, and relational patterns.
- `switch` expressions.
- Nullable operators: `?.`, `??`, `??=`, and `!`.

**Build**
- Write the first domain types as records: `EventId`, `Money`, and `SeatCount`.
- Change one `if`/`else` chain into a `switch` expression.

**Check**
- When do you use a `record`, and when do you use a `class`?
- What is the difference between `required` and a constructor parameter?

---

### Day 3 — C# syntax refresh, part 2

**Learn**
- LINQ methods: `Select`, `Where`, `GroupBy`, `Any`, `All`, `Aggregate`, `SelectMany`.
- Deferred execution. This is the cause of many bugs.
- Extension methods.
- Generics and constraints. Generic math is not necessary now.
- `IEnumerable<T>`, `IReadOnlyList<T>`, and `IQueryable<T>`. Know the difference.
- Tuples and deconstruction.
- `Span<T>` and `ReadOnlySpan<T>`. Learn the idea only.
- `IDisposable`, `IAsyncDisposable`, and the `using` declaration.

**Build**
- Write LINQ queries against a list of events in memory.
- Write one extension method for a validation.

**Check**
- Why can an `IQueryable<T>` cause a bad SQL query?

---

### Day 4 — Asynchronous code

**Learn**
- `async` and `await`. The state machine. The thread does not block.
- `Task`, `Task<T>`, and `ValueTask<T>`.
- `CancellationToken`. Send it to all asynchronous methods.
- `Task.WhenAll` and `Task.WhenAny`.
- `IAsyncEnumerable<T>` and `await foreach`.
- Bad practices: `async void`, `.Result`, and `.Wait()`. Do not use them.
- `ConfigureAwait(false)`. It is not necessary in ASP.NET Core.

**Build**
- Make all service methods asynchronous.
- Add a `CancellationToken` parameter to each of them.

**Check**
- What happens if an `async void` method throws an exception?

---

### Day 5 — Dependency injection, configuration, and logging

**Learn**
- The built-in DI container. `AddSingleton`, `AddScoped`, and `AddTransient`.
- Captive dependencies. A singleton must not hold a scoped service.
- Keyed services (`AddKeyedScoped`).
- The configuration system: `appsettings.json`, environment variables, and user secrets.
- The Options pattern: `IOptions<T>`, `IOptionsSnapshot<T>`, and validation at start.
- `ILogger<T>` and structured logging. Use message templates, not string interpolation.
- Log levels and log scopes.
- Serilog as an alternative sink.

**Build**
1. Register the services of the application in an extension method, `AddApplication()`.
2. Move the connection string to user secrets.
3. Add a typed `EventHubOptions` class with data annotations and `ValidateOnStart`.
4. Add structured logs to the booking flow.

**Check**
- Which lifetime does a `DbContext` use? Why?

---

### Day 6 — The ASP.NET Core request pipeline

**Learn**
- Middleware and the order of middleware. The order controls the behaviour.
- Custom middleware and `IMiddleware`.
- Controllers against minimal APIs. Learn both. Minimal APIs are common in new code.
- Route groups (`MapGroup`) and endpoint filters.
- Model binding and model validation.
- Problem Details (RFC 9457) and `IExceptionHandler` for global error handling.
- OpenAPI. .NET 10 has built-in OpenAPI support. Swashbuckle is not necessary.
- API versioning with `Asp.Versioning`.
- CORS and rate limiting.

**Build**
1. Add the endpoints for events with minimal APIs in a route group.
2. Add a global exception handler that returns Problem Details.
3. Add a `/health` endpoint.
4. Open the OpenAPI document in Scalar or Swagger UI.

**Check**
- What happens if you put the authentication middleware after the endpoints?

---

### Day 7 — EF Core with PostgreSQL, part 1

**Learn**
- The `Npgsql.EntityFrameworkCore.PostgreSQL` provider.
- `DbContext`, `DbSet<T>`, and the change tracker.
- Configuration with `IEntityTypeConfiguration<T>`. Do not use data annotations for the model.
- Relationships: one-to-many, many-to-many, and owned types.
- Migrations: `dotnet ef migrations add`, `dotnet ef database update`.
- Value converters and value objects.
- `AsNoTracking()` for read queries.

**Build**
1. Start PostgreSQL in Docker with a `docker-compose.yml` file.
2. Add the `EventHubDbContext` class with `Event` and `Booking` entities.
3. Write the entity configuration classes.
4. Create the first migration. Apply it to the database.
5. Look at the generated SQL in the logs.

**Check**
- Where does EF Core keep the migration history?

---

### Day 8 — EF Core with PostgreSQL, part 2

**Learn**
- The N+1 problem. Use `Include`, split queries, or projections.
- Projection to a DTO with `Select`. This is the best method for read queries.
- Transactions and `SaveChangesAsync`. One `SaveChanges` call is one transaction.
- Optimistic concurrency with `xmin` in PostgreSQL.
- Indexes, unique constraints, and check constraints.
- Bulk operations: `ExecuteUpdateAsync` and `ExecuteDeleteAsync`.
- Interceptors for audit fields.
- Raw SQL when LINQ is not sufficient.

**Build**
1. Add a concurrency token to `Event`. Stop two bookings for the last seat.
2. Add an interceptor that fills `CreatedAtUtc` and `UpdatedAtUtc`.
3. Find one N+1 query in your logs. Correct it.

**Check**
- Which method do you use for a read-only list endpoint? Why?

---

### Day 9 — Architecture and the folder structure

**Learn**
- Layered architecture, Clean Architecture, and Vertical Slice Architecture.
- The dependency rule: the inner layers do not know the outer layers.
- Domain entities against DTOs. Never send an entity to the client.
- The repository pattern. The `DbContext` is already a unit of work. Do not add a repository without a reason.
- The Result pattern as an alternative to exceptions for business rules.
- Manual mapping against AutoMapper. Manual mapping is clear and fast.

**Build**
Change the solution to four projects.

```
EventHub/
├─ src/
│  ├─ EventHub.Domain/          # entities, value objects, domain rules. No packages.
│  ├─ EventHub.Application/     # use cases, DTOs, interfaces
│  ├─ EventHub.Infrastructure/  # EF Core, external systems
│  └─ EventHub.Api/             # endpoints, middleware, DI setup
└─ tests/
   ├─ EventHub.UnitTests/
   └─ EventHub.IntegrationTests/
```

Dependencies: `Api` → `Infrastructure` → `Application` → `Domain`.

**Check**
- Can `Domain` reference EF Core? Why not?

---

### Day 10 — Use cases, validation, and cross-cutting behaviour

**Learn**
- CQRS as an idea. Separate the write model from the read model.
- The mediator pattern. MediatR is not free now. Write a small mediator, or use a free package such as Wolverine.
- Pipeline behaviours for logging, validation, and transactions.
- FluentValidation.
- Domain events inside one process.

**Build**
1. Make one command class and one handler class for each use case, for example `CreateBookingCommand`.
2. Add a validation behaviour that runs before each handler.
3. Raise a `BookingCreated` domain event. Handle it in the same process.

**Check**
- What advantage does a pipeline behaviour give against a base class?

---

### Day 11 — Authentication, authorization, and testing

**Learn**
- JWT bearer tokens. Access tokens and refresh tokens.
- ASP.NET Core Identity, or a third-party identity server such as Keycloak.
- Policy-based authorization and claims. Do not use role checks in the code.
- Password hashing. Never keep a plain password.
- xUnit, and assertions with Shouldly or AwesomeAssertions.
- Test doubles with NSubstitute.
- Integration tests with `WebApplicationFactory` and Testcontainers for PostgreSQL.

**Build**
1. Add JWT authentication and a `CanCancelBooking` policy.
2. Write unit tests for the seat rules in the domain.
3. Write one integration test that creates an event and books a seat against a real PostgreSQL container.

**Check**
- Why is a test against a real database better than a test against an in-memory provider?

---

## Stage 2 — The modular monolith (Day 12 to Day 15)

A modular monolith is one deployable process. Inside, the modules stay independent. This is the correct start for almost all systems. Do not start with microservices.

### Day 12 — Module boundaries

**Learn**
- What makes a good boundary. Follow the business capability, not the technical layer.
- Public contracts against internal types. Use the `internal` keyword.
- One database, but one schema for each module. Do not join across schemas.
- In-process integration events between modules.
- Architecture tests with NetArchTest. They stop bad references.

**Build**
Change the folder structure.

```
EventHub/
├─ src/
│  ├─ Api/
│  │  └─ EventHub.Api/                    # the only host
│  ├─ Modules/
│  │  ├─ Events/
│  │  │  ├─ EventHub.Modules.Events.Domain/
│  │  │  ├─ EventHub.Modules.Events.Application/
│  │  │  ├─ EventHub.Modules.Events.Infrastructure/
│  │  │  └─ EventHub.Modules.Events.Contracts/      # public. Other modules use this only.
│  │  ├─ Bookings/
│  │  └─ Notifications/
│  └─ Common/
│     ├─ EventHub.Common.Domain/
│     └─ EventHub.Common.Infrastructure/
└─ tests/
```

**Check**
- Which project can the Bookings module reference in the Events module?

---

### Day 13 — Background work and messages inside the process

**Learn**
- `BackgroundService` and `IHostedService`.
- `System.Threading.Channels` for a producer and a consumer in memory.
- Scheduled jobs with Quartz.NET or Hangfire.
- The Outbox pattern. Write the event and the data in one transaction.
- Idempotency. A handler can run two times.

**Build**
1. Add an outbox table to the Bookings module.
2. Add a background service that reads the outbox and calls the Notifications module.
3. Make the notification handler idempotent with a message identifier.

**Check**
- Why can you lose an event if you send it directly in the handler?

---

### Day 14 — Performance, caching, and resilience

**Learn**
- `HybridCache` (new in .NET 9). It replaces most `IMemoryCache` and Redis code.
- Cache keys, expiration, and cache invalidation.
- Output caching and response compression.
- `IHttpClientFactory`. Never make a `new HttpClient()`.
- `Microsoft.Extensions.Http.Resilience` for retry, timeout, and circuit breaker. It uses Polly.
- Rate limiting middleware.
- Benchmarks with BenchmarkDotNet. Measure. Do not guess.

**Build**
1. Add `HybridCache` to the event list endpoint. Use Redis in Docker as the second level.
2. Remove the cache entry when an event changes.
3. Add rate limiting to the booking endpoint.

**Check**
- What problem does a circuit breaker solve that a retry does not solve?

---

### Day 15 — Containers, observability, and the pipeline

**Learn**
- A multi-stage `Dockerfile`. Or use `dotnet publish /t:PublishContainer` with no Dockerfile.
- `docker compose` for the API, PostgreSQL, and Redis.
- OpenTelemetry: traces, metrics, and logs.
- Correlation identifiers across all logs.
- .NET Aspire for local orchestration and a dashboard.
- Health checks: liveness and readiness.
- GitHub Actions: build, test, and publish.

**Build**
1. Put the full stack in `docker compose`.
2. Add OpenTelemetry. Send the traces to the Aspire dashboard or to Jaeger.
3. Add a GitHub Actions workflow that runs the tests.

**Check**
- What is the difference between a liveness probe and a readiness probe?

---

## Stage 3 — Microservices in a monorepo (Day 16 to Day 20)

Stage 2 gave you modules with clear borders. Now move each module to its own process.

### Day 16 — The monorepo structure

**Learn**
- A monorepo keeps many deployable units in one Git repository. It is a repository strategy, not an architecture.
- Solution filters (`.slnf`). They keep the load time short.
- `Directory.Build.props` and `Directory.Packages.props` at the root. One version for each package.
- Shared contract packages. Keep them small.
- Path filters in CI. Build only the service that changed.

**Build**

```
eventhub/
├─ .github/workflows/
├─ Directory.Build.props
├─ Directory.Packages.props
├─ EventHub.sln
├─ build/                       # scripts, Dockerfiles
├─ deploy/                      # compose files, Kubernetes manifests
├─ docs/                        # decision records (ADRs)
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
│     ├─ EventHub.Shared.Contracts/       # message and DTO definitions
│     └─ EventHub.Shared.Kernel/          # base types, Result, errors
└─ tests/
   ├─ Events.UnitTests/
   ├─ Events.IntegrationTests/
   └─ System.Tests/
```

**Check**
- What must you never put in a shared project?

---

### Day 17 — Service separation and communication

**Learn**
- One database for each service. This is the most important rule. Give each service its own PostgreSQL database.
- Synchronous communication: REST with a typed `HttpClient`, or gRPC for internal calls.
- Asynchronous communication: messages. Prefer this.
- Data duplication. The Bookings service keeps a copy of the event title. This is correct.
- Contract-first design.

**Build**
1. Move the Events module to `EventHub.Events.Api` with its own database.
2. Move the Bookings module to `EventHub.Bookings.Api` with its own database.
3. Let Bookings call Events over gRPC to confirm that an event exists.
4. Add resilience to the client.

**Check**
- What happens to bookings if the Events service stops? Is that acceptable?

---

### Day 18 — Message-based integration

**Learn**
- RabbitMQ, and MassTransit as the abstraction.
- Publish and subscribe against send and receive.
- Consumers, retry policies, and dead-letter queues.
- The transactional outbox in MassTransit.
- Eventual consistency. The data is correct after a short delay.
- Event versioning. Add fields. Do not remove fields.

**Build**
1. Add RabbitMQ to `docker compose`.
2. Publish a `BookingConfirmed` event from Bookings.
3. Consume it in Notifications and in Events (to change the seat count).
4. Turn off the Notifications service. Confirm that no message is lost.

**Check**
- Why must a consumer be idempotent?

---

### Day 19 — The gateway, security, and sagas

**Learn**
- YARP as a reverse proxy and an API gateway.
- Authentication at the gateway. Send the identity to the services.
- Distributed tracing across services with OpenTelemetry.
- The saga pattern for a process across many services.
- Compensating actions. There is no distributed transaction.
- Service discovery. Use DNS in Docker or in Kubernetes.

**Build**
1. Add a YARP gateway. Route `/events` and `/bookings` to the correct service.
2. Validate the JWT at the gateway.
3. Add a saga for the booking process: reserve the seat, take the payment, confirm the booking.
4. Add a compensating action when the payment fails.

**Check**
- Follow one request in the trace view. Do you see all three services?

---

### Day 20 — Delivery and review

**Learn**
- Migrations in production. Do not run `Update-Database` at start in a system with many instances.
- Configuration and secrets for each environment.
- Blue-green and canary releases. Learn the idea.
- Kubernetes basics: deployment, service, ingress, and config map.
- Structured logs in a central place.

**Build**
1. Write a `README.md` file. Add a diagram of the architecture.
2. Write three decision records in `docs/`. Give the reason for three of your choices.
3. Add a CI job that builds a container image for each service.
4. Read your Day 1 code. Note what you would change now.

**Check**
- Can a new developer start the full system with one command?

---

## Folder structure — a short comparison

| Term | What it means | When to use it |
|---|---|---|
| **Monolith** | One deployable unit. One process. | Almost all new systems. Your Stage 1. |
| **Modular monolith** | One deployable unit. Independent modules inside. | The best default. Your Stage 2. |
| **Microservices** | Many deployable units. One database for each. | Only when teams or scale need it. Your Stage 3. |
| **Monorepo** | One Git repository for many projects. | A repository choice. It works with all the types above. |
| **Polyrepo** | One Git repository for each service. | Large independent teams. |

Note the difference. **Monolith** and **microservices** describe the deployment. **Monorepo** and **polyrepo** describe the source control. A monorepo can hold a monolith. A monorepo can hold 50 microservices.

---

## Standard rules to follow in all stages

- Use folders that follow the feature, not the type. Use `Features/Bookings/`, not `Services/`, `Helpers/`, and `Managers/`.
- Never name a class `Helper`, `Manager`, `Util`, or `Common`. These names hide the purpose.
- Keep `Program.cs` short. Move the setup to extension methods.
- One public type in one file. The file name is the type name.
- Make types `internal` and `sealed` if there is no reason to make them public.
- Do not put business rules in a controller or an endpoint.
- Never return an EF Core entity from an endpoint.
- Use `async` for all input and output work.
- Use UTC for all times. Use `DateTimeOffset`, or `DateOnly` and `TimeOnly`.
- Turn on nullable reference types. Do not use `!` to hide a warning.
- Turn on `TreatWarningsAsErrors`.

---

## Sources to use

- Microsoft Learn — the .NET and ASP.NET Core documentation. This is the first source.
- The .NET Blog — for the "What is new in .NET 10" and "What is new in C# 14" pages.
- The `dotnet/eShop` repository on GitHub — a reference microservices sample.
- Steve Ardalis Smith — Clean Architecture template.
- Jimmy Bogard's blog — vertical slices and messaging.
- Nick Chapsas and Milan Jovanović — video content on current practice.

---

## What to do if you have less time

Do Day 1 to Day 11 only. A developer who writes a clean single service with good tests has more value than a developer who builds microservices with no reason. Stage 3 is knowledge for interviews and for the future. Stage 1 is your daily work.
