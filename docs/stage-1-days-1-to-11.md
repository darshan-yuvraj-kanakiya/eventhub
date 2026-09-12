# Stage 1 — Modern C# and one Web API (Day 1 to Day 11)

This is the detailed guide. The short roadmap gives the plan. This file gives the content.

Each day has these parts:
- **Why this day** — the reason for the topic.
- **What changed** — the difference from the .NET Core code that you wrote 2 years ago.
- **Concepts** — the material, with code.
- **Build** — the steps in the EventHub project.
- **Common mistakes** — the errors that developers make.
- **Check** — questions to answer without help.

---

# Day 1 — The platform, the CLI, and the project skeleton

## Why this day

You must remove the old habits first. Many things that you did in .NET Core 3.1 or .NET 5 are wrong now. This day gives you a correct empty base. All other days build on it.

## What changed

| Old (2020 to 2022) | Now (.NET 10) |
|---|---|
| `Startup.cs` with `ConfigureServices` and `Configure` | One `Program.cs` file. Top-level statements. |
| `Main(string[] args)` with a class | No class. No `Main`. Write the statements directly. |
| `using System;` at the top of each file | Implicit usings. The SDK adds them. |
| `namespace X { class Y { } }` | `namespace X;` on one line. No braces. No indent. |
| A package version in each `.csproj` file | One version in `Directory.Packages.props`. |
| `IHostBuilder` and `ConfigureWebHostDefaults` | `WebApplication.CreateBuilder(args)`. |
| Swashbuckle for OpenAPI | Built-in OpenAPI (`AddOpenApi`). |

## Concepts

### The SDK and the runtime

- The **SDK** builds the code. The **runtime** runs the code. The SDK includes a runtime.
- `dotnet --info` shows all installed versions.
- A `global.json` file locks the SDK version for the repository. Add it in a team.

```json
{
  "sdk": {
    "version": "10.0.100",
    "rollForward": "latestFeature"
  }
}
```

### The CLI commands that you need each day

| Command | Function |
|---|---|
| `dotnet new list` | Show all templates. |
| `dotnet new sln -n EventHub` | Make a solution file. |
| `dotnet new webapi -n X --use-controllers` | Make a Web API. Remove the flag for minimal APIs. |
| `dotnet sln add src/**/*.csproj` | Add projects to the solution. |
| `dotnet add reference ../Other/Other.csproj` | Add a project reference. |
| `dotnet add package Serilog.AspNetCore` | Add a NuGet package. |
| `dotnet build` / `dotnet run` / `dotnet test` | Build, run, test. |
| `dotnet watch` | Run, and reload after each file change. |
| `dotnet format` | Correct the code style. |

### The modern project file

The old `.csproj` file listed each source file. The SDK-style file does not. It includes all `.cs` files under the folder.

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
</Project>
```

- `Microsoft.NET.Sdk` — a library or a console application.
- `Microsoft.NET.Sdk.Web` — a web application.
- `Nullable` — the compiler warns you about a possible `null` value.
- `ImplicitUsings` — the SDK adds the usual `using` lines.

### Top-level statements

The new `Program.cs` file:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapGet("/ping", () => "pong");

app.Run();
```

Note these points:
- The variable `builder` has two parts. `builder.Services` registers the services. `builder.Configuration` reads the settings.
- After `builder.Build()`, you cannot add a service. The container is closed.
- The lines after `app` are the middleware and the endpoints. The order is important.
- `app.Run()` starts the server and blocks the thread.

### Nullable reference types

The compiler now separates `string` from `string?`.

```csharp
string name = null;      // warning CS8600
string? maybe = null;    // correct
int length = maybe.Length;       // warning CS8602. maybe can be null.
int length2 = maybe!.Length;     // no warning. But you take the risk.
int length3 = maybe?.Length ?? 0; // correct
```

This is only a compiler check. At run time, nothing changes. But the warnings find real bugs. Turn them on.

### Central Package Management

Put this file at the root of the repository. It sets one version for all projects.

`Directory.Packages.props`:

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup>
    <PackageVersion Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.0.0" />
    <PackageVersion Include="Serilog.AspNetCore" Version="9.0.0" />
  </ItemGroup>
</Project>
```

Then a project file has no version:

```xml
<PackageReference Include="Serilog.AspNetCore" />
```

### Shared build settings

`Directory.Build.props` at the root applies to all projects below it.

```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
    <LangVersion>latest</LangVersion>
  </PropertyGroup>
</Project>
```

`TreatWarningsAsErrors` looks hard. It is the fastest teacher. The build stops. You correct the cause.

## Build

1. Install the .NET 10 SDK. Run `dotnet --info`. Confirm that the SDK version starts with `10.`.
2. Make the folder `eventhub`. Run `git init`.
3. Run `dotnet new gitignore`.
4. Run `dotnet new editorconfig`.
5. Run `dotnet new sln -n EventHub`.
6. Run `dotnet new webapi -o src/EventHub.Api -n EventHub.Api`.
7. Run `dotnet sln add src/EventHub.Api/EventHub.Api.csproj`.
8. Make `Directory.Build.props` and `Directory.Packages.props` at the root. Use the content above.
9. Remove the duplicate properties from `EventHub.Api.csproj`. The root file gives them now.
10. Delete the sample weather forecast code.
11. Add a `GET /ping` endpoint.
12. Run `dotnet watch --project src/EventHub.Api`.
13. Change the text of the endpoint. Save the file. Confirm that the browser shows the new text.
14. Commit the work.

## Common mistakes

- You install the SDK, but an old `global.json` file forces an old version. Then the build fails with a confusing message.
- You add a service after `builder.Build()`. You get an exception at run time.
- You set `Nullable` to `disable` because there are many warnings. Do not do this. Correct the warnings.

## Check

- What is the difference between `builder.Services` and `app`?
- Where does `dotnet run` put the compiled output?
- Why does a `Directory.Packages.props` file help a big repository?

---

# Day 2 — C# syntax refresh, part 1: types and data

## Why this day

Your domain model uses these features. If you write C# 7 style code, the code is three times longer, and it has more bugs.

## What changed

C# 9 to C# 14 added many features for data types. The most important ones are records, pattern matching, and `required` members.

## Concepts

### Records

A record is a class (or a struct) with value equality. The compiler writes the equality, the hash code, and the `ToString` method.

```csharp
public record Money(decimal Amount, string Currency);

var a = new Money(100, "INR");
var b = new Money(100, "INR");

a == b;              // true. A class gives false here.
a.ToString();        // Money { Amount = 100, Currency = INR }
var c = a with { Amount = 200 };   // a copy with one change
```

Rules:
- Use a `record` for data that has no identity: DTOs, commands, events, and value objects.
- Use a `class` for an entity that has an identity, for example `Event` with an `Id`.
- `record struct` is a value type. Use it for very small data such as an identifier.

A record can have a body:

```csharp
public record Money(decimal Amount, string Currency)
{
    public static Money Zero(string currency) => new(0, currency);
    public Money Add(Money other)
    {
        if (other.Currency != Currency)
        {
            throw new InvalidOperationException("The currencies are different.");
        }
        return this with { Amount = Amount + other.Amount };
    }
}
```

### `init` and `required`

```csharp
public class EventDto
{
    public required string Title { get; init; }
    public required DateTimeOffset StartsAt { get; init; }
    public string? Description { get; init; }
}

var dto = new EventDto { Title = "Meetup", StartsAt = DateTimeOffset.UtcNow };
dto.Title = "New";   // error. init allows a value only in the initializer.
```

- `init` — you set the value one time, at the moment of creation.
- `required` — the compiler makes an error if the initializer does not set the property.

Together they replace a long constructor. The caller sees the property names.

### Primary constructors (C# 12)

A class can now take constructor parameters in the declaration.

```csharp
public class BookingService(IBookingRepository repository, ILogger<BookingService> logger)
{
    public async Task CancelAsync(Guid id, CancellationToken ct)
    {
        logger.LogInformation("Cancel booking {BookingId}", id);
        await repository.RemoveAsync(id, ct);
    }
}
```

The old code needed four extra lines: two fields, a constructor, and two assignments. Use this for all services with dependency injection.

Note: the parameter is in scope in the full class. It is not a `readonly` field. Do not change it.

### Collection expressions (C# 12)

```csharp
int[] numbers = [1, 2, 3];
List<string> names = ["Amit", "Priya"];
Span<int> span = [1, 2, 3];

int[] more = [0, ..numbers, 4];   // the spread operator: [0, 1, 2, 3, 4]
```

One syntax for all collection types. The compiler chooses the fastest method.

### Pattern matching

This is the largest change in the language. Learn it well.

```csharp
// type pattern
if (shape is Circle c) { ... }

// property pattern
if (booking is { Status: BookingStatus.Confirmed, Seats: > 2 }) { ... }

// relational and logical patterns
var category = seats switch
{
    <= 0 => "invalid",
    < 50 => "small",
    < 200 => "medium",
    _ => "large"
};

// list pattern
int[] values = [1, 2, 3];
if (values is [1, .., 3]) { ... }   // starts with 1, ends with 3

// combined
var fee = booking switch
{
    { Seats: 0 } => 0m,
    { Event.IsFree: true } => 0m,
    { Seats: var s } when s > 10 => s * 80m,
    { Seats: var s } => s * 100m
};
```

A `switch` expression must cover all cases. The compiler warns you if it does not. Use `_` for the rest.

### Other small features

```csharp
// target-typed new
Dictionary<string, List<int>> map = new();

// null-coalescing assignment
name ??= "unknown";

// raw string literals (C# 11)
var json = """
    { "title": "Meetup", "seats": 50 }
    """;

// the field keyword (C# 14) — no backing field is necessary
public string Title
{
    get => field;
    set => field = value.Trim();
}
```

## Build

1. Make the folder `Domain` in the API project. You divide the project into real projects on Day 9.
2. Write these types:

```csharp
public readonly record struct EventId(Guid Value)
{
    public static EventId New() => new(Guid.CreateVersion7());
}

public record Money(decimal Amount, string Currency);

public enum BookingStatus { Pending, Confirmed, Cancelled }

public class Event
{
    public EventId Id { get; private set; } = EventId.New();
    public required string Title { get; set; }
    public required DateTimeOffset StartsAt { get; set; }
    public required int TotalSeats { get; set; }
    public int BookedSeats { get; private set; }

    public int AvailableSeats => TotalSeats - BookedSeats;
}
```

3. Write a `switch` expression that gives a text status for an event: "cancelled", "sold out", "closing soon", or "open".

Note: `Guid.CreateVersion7()` makes a sequential identifier. It is much better than `Guid.NewGuid()` for a database primary key, because the index does not fragment.

## Common mistakes

- You use a `record` for an EF Core entity. Then two different rows with the same values look equal. Use a `class` for an entity.
- You make all properties `init`, and then EF Core cannot set them. EF Core can use `init`, but a private setter is safer for an entity.
- You write a `switch` expression with no `_` case, and it throws at run time.

## Check

- Explain value equality against reference equality with one example.
- Why is `record struct` good for an identifier type?
- Rewrite an `if`/`else if`/`else` block as a `switch` expression.

---

# Day 3 — C# syntax refresh, part 2: LINQ and behaviour

## Why this day

Your data access is LINQ. If you do not know deferred execution, you will write slow queries and get run-time errors.

## Concepts

### Deferred execution

A LINQ query does not run when you write it. It runs when you read it.

```csharp
var query = events.Where(e => e.AvailableSeats > 0);   // nothing happens here
var list = query.ToList();                             // the query runs here
```

These methods run the query: `ToList`, `ToArray`, `ToDictionary`, `First`, `Single`, `Count`, `Any`, `Sum`, and `foreach`.

Why this is important:
- If you enumerate a query two times, the work happens two times.
- With EF Core, the query goes to the database at the moment of execution. If you close the `DbContext` first, you get an exception.

### `IEnumerable<T>` against `IQueryable<T>`

This is the most costly mistake in EF Core.

```csharp
// BAD. ToList() reads the full table. The filter runs in memory.
var open = context.Events.ToList().Where(e => e.AvailableSeats > 0);

// GOOD. The filter becomes a SQL WHERE clause.
var open = await context.Events.Where(e => e.AvailableSeats > 0).ToListAsync();
```

- `IEnumerable<T>` — the data is in memory. The delegate runs in C#.
- `IQueryable<T>` — the data is in the database. The expression becomes SQL.

Rule: keep the type `IQueryable<T>` until the last moment. Do not return `IEnumerable<T>` from a method that a caller will filter.

### The LINQ methods that you use most

```csharp
events.Where(e => e.StartsAt > now)                  // filter
      .OrderBy(e => e.StartsAt).ThenBy(e => e.Title) // sort
      .Select(e => new EventListItem(e.Id, e.Title)) // project
      .Skip(20).Take(10)                             // page
      .ToListAsync(ct);

bookings.GroupBy(b => b.EventId)
        .Select(g => new { EventId = g.Key, Total = g.Sum(b => b.Seats) });

events.Any(e => e.Title == title);        // does one exist?
events.All(e => e.TotalSeats > 0);        // are all true?
events.SelectMany(e => e.Bookings);       // flatten a list of lists
events.FirstOrDefault(e => e.Id == id);   // the first, or null
events.SingleOrDefault(e => e.Id == id);  // exactly one, or null. It throws if two exist.
```

`First` against `Single`: `Single` reads two rows to confirm that only one exists. Use `First` when the order gives the answer. Use `Single` when you must prove that the data is unique.

### Extension methods

```csharp
public static class QueryableExtensions
{
    public static IQueryable<T> Page<T>(this IQueryable<T> query, int page, int size)
        => query.Skip((page - 1) * size).Take(size);
}

// use
var items = await context.Events.Page(2, 20).ToListAsync(ct);
```

C# 14 adds extension blocks. They allow extension properties too. Read about them, but the old syntax still works everywhere.

### Collections: choose the correct type

| Type | Use |
|---|---|
| `IEnumerable<T>` | A parameter. You read the items one time. |
| `IReadOnlyList<T>` | A return value. The caller must not change it. |
| `List<T>` | Inside a method. |
| `Dictionary<TKey, TValue>` | A lookup by key. |
| `HashSet<T>` | A test for existence. No duplicates. |
| `FrozenDictionary<T>` | A lookup that never changes. It is faster to read. |

Do not return `List<T>` from a public method. The caller can then change your internal state.

### Disposal

```csharp
// the using declaration. It disposes at the end of the block.
using var stream = File.OpenRead(path);

// for asynchronous disposal
await using var connection = new NpgsqlConnection(connectionString);
```

## Build

1. Make a list of 20 sample events in memory.
2. Write these queries:
   - The events of the next 7 days, sorted by the start time.
   - The count of events for each month.
   - The event with the fewest free seats.
   - A page of 5 events, page number 2.
3. Write a `Page` extension method.
4. Write one query in two ways. Compare `.ToList().Where(...)` against `.Where(...).ToList()`. Measure the difference with a stopwatch on 100000 items.

## Common mistakes

- You call `.Count()` in a loop condition. The query runs on each pass.
- You call `.ToList()` early "to be safe". You read the full table.
- You return `IQueryable<T>` from a repository to a controller. The database context can be closed. The controller can also write bad queries.

## Check

- What is the output of a query that you enumerate two times over a `Random` source?
- When does `SingleOrDefault` throw?

---

# Day 4 — Asynchronous code

## Why this day

All input and output in ASP.NET Core is asynchronous. A mistake here does not fail the tests. It fails in production, under load.

## Concepts

### What `await` does

`await` does **not** make a new thread. It does this:
1. The method starts the input or output operation.
2. The method returns the thread to the thread pool.
3. The operating system tells .NET when the operation is complete.
4. .NET takes a thread from the pool and continues the method.

The result: the server holds many requests with few threads.

```csharp
public async Task<Event?> GetAsync(EventId id, CancellationToken ct)
{
    return await context.Events.FirstOrDefaultAsync(e => e.Id == id, ct);
}
```

### The rules

**1. Never use `async void`.**

```csharp
public async void Handle()   // BAD. You cannot catch the exception. The process can stop.
public async Task HandleAsync()  // GOOD
```

The only exception is an event handler in a UI application.

**2. Never use `.Result` or `.Wait()`.**

They block the thread. In some contexts they cause a deadlock. In ASP.NET Core they cause thread pool starvation. The application becomes slow and then it stops.

**3. Send the `CancellationToken` everywhere.**

```csharp
app.MapGet("/events", async (IEventService service, CancellationToken ct) =>
{
    return await service.GetAllAsync(ct);
});
```

ASP.NET Core gives the token. If the client closes the connection, the token is cancelled. Your database query stops. This saves resources.

**4. Do work in parallel when the work is independent.**

```csharp
// SLOW. 200 ms total.
var user = await GetUserAsync(id, ct);        // 100 ms
var events = await GetEventsAsync(id, ct);    // 100 ms

// FAST. 100 ms total.
var userTask = GetUserAsync(id, ct);
var eventsTask = GetEventsAsync(id, ct);
await Task.WhenAll(userTask, eventsTask);
var user = await userTask;
var events = await eventsTask;
```

Warning: you cannot do this with the same `DbContext`. A `DbContext` is not thread safe. Use `IDbContextFactory<T>` for parallel queries.

**5. Do not make a task for work that has no input or output.**

Do not put fast CPU work in `Task.Run` in a web application. It does not help. It adds cost.

### `Task` against `ValueTask`

- `Task<T>` — the default. Use it.
- `ValueTask<T>` — use it only in a very hot path where the result is often available immediately, for example a cache hit. You can await a `ValueTask` one time only.

### `IAsyncEnumerable<T>`

Stream the results. Do not load all of them into memory.

```csharp
public async IAsyncEnumerable<EventDto> StreamAsync([EnumeratorCancellation] CancellationToken ct)
{
    await foreach (var e in context.Events.AsAsyncEnumerable().WithCancellation(ct))
    {
        yield return new EventDto(e.Id, e.Title);
    }
}
```

ASP.NET Core can return this type directly. It streams the JSON to the client.

### Exceptions

```csharp
try
{
    await Task.WhenAll(task1, task2);
}
catch (Exception ex)
{
    // WhenAll throws the FIRST exception only.
    // Read task1.Exception and task2.Exception for all of them.
}
```

Catch `OperationCanceledException` separately. A cancellation is not an error.

## Build

1. Make all service methods asynchronous. Add `CancellationToken ct` as the last parameter.
2. Add an endpoint that reads two independent things in parallel.
3. Write a test: start a request, cancel it after 100 ms, and confirm that the log shows the cancellation.
4. Find any `.Result` or `.Wait()` call in your code. Remove it.

## Common mistakes

- A method has the name `GetAsync`, but it is not asynchronous inside. Use the suffix `Async` only for a real `Task` method.
- You use `async` and `await` in a method that only returns another task. It works, but it adds a state machine. You can return the task directly. But keep `await` if you have a `using` block.
- You catch and swallow `OperationCanceledException` as an error, and you log noise.

## Check

- Explain thread pool starvation to a junior developer.
- Why is `Task.WhenAll` with one `DbContext` dangerous?

---

# Day 5 — Dependency injection, configuration, and logging

## Why this day

These three systems appear in every file that you write. You must know them well.

## Concepts

### The lifetimes

| Lifetime | Instances | Use for |
|---|---|---|
| `AddSingleton` | One for the application | Cache, configuration, a stateless helper |
| `AddScoped` | One for each HTTP request | `DbContext`, a service that holds request state |
| `AddTransient` | One for each injection | A small stateless object |

### The captive dependency problem

```csharp
builder.Services.AddSingleton<CacheService>();   // lives forever
builder.Services.AddScoped<AppDbContext>();      // lives for one request
```

If `CacheService` takes an `AppDbContext`, the context lives forever. It is now shared between requests. This causes errors that you cannot repeat.

The default container finds this error at start in the Development environment. Keep this check on:

```csharp
builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateScopes = true;
    options.ValidateOnBuild = true;
});
```

To use a scoped service inside a singleton, make a scope:

```csharp
using var scope = serviceScopeFactory.CreateScope();
var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
```

### Registration methods

```csharp
builder.Services.AddScoped<IEventService, EventService>();          // interface to class
builder.Services.AddScoped<EventService>();                          // the class only
builder.Services.TryAddScoped<IEventService, EventService>();        // only if none exists
builder.Services.AddScoped<IEventService>(sp => new EventService()); // a factory
builder.Services.AddKeyedScoped<INotifier, EmailNotifier>("email");  // keyed (new in .NET 8)
```

Use a keyed service like this:

```csharp
app.MapPost("/notify", ([FromKeyedServices("email")] INotifier notifier) => ...);
```

### Group the registration

Do not put 60 lines in `Program.cs`.

```csharp
// in EventHub.Infrastructure
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(o =>
            o.UseNpgsql(configuration.GetConnectionString("Default")));
        services.AddScoped<IEventRepository, EventRepository>();
        return services;
    }
}

// in Program.cs
builder.Services.AddInfrastructure(builder.Configuration);
```

### Configuration

The sources load in this order. A later source replaces an earlier source.

1. `appsettings.json`
2. `appsettings.{Environment}.json`
3. User secrets (Development only)
4. Environment variables
5. Command line arguments

The environment comes from the `ASPNETCORE_ENVIRONMENT` variable. The values are `Development`, `Staging`, and `Production`.

A nested key uses a colon. In an environment variable, use two underscores, because Linux does not allow a colon.

```
ConnectionStrings__Default=Host=localhost;Database=eventhub
```

**Never put a secret in `appsettings.json`.** Use user secrets on your machine:

```bash
dotnet user-secrets init --project src/EventHub.Api
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;..."
```

### The Options pattern

Do not inject `IConfiguration`. Make a typed class.

```csharp
public sealed class BookingOptions
{
    public const string SectionName = "Booking";

    [Range(1, 100)]
    public int MaxSeatsPerBooking { get; init; } = 10;

    [Required]
    public string SupportEmail { get; init; } = string.Empty;
}

// registration
builder.Services.AddOptions<BookingOptions>()
    .Bind(builder.Configuration.GetSection(BookingOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// use
public class BookingService(IOptions<BookingOptions> options)
{
    private readonly BookingOptions _options = options.Value;
}
```

`ValidateOnStart` is important. The application fails at start, not at the first request.

- `IOptions<T>` — read one time. Singleton.
- `IOptionsSnapshot<T>` — read again for each request. Scoped.
- `IOptionsMonitor<T>` — read again, with a change notification. Singleton.

### Logging

Use message templates. Do not use string interpolation.

```csharp
// BAD. The log system sees one long string. You cannot search by the identifier.
logger.LogInformation($"Booking {id} was cancelled by {userId}");

// GOOD. The log system stores BookingId and UserId as fields.
logger.LogInformation("Booking {BookingId} was cancelled by {UserId}", id, userId);
```

Levels: `Trace`, `Debug`, `Information`, `Warning`, `Error`, `Critical`.

- `Information` — a business event. "The booking is confirmed."
- `Warning` — something is not correct, but the system continues.
- `Error` — one operation failed.
- `Critical` — the application cannot continue.

Do not log at `Information` inside a loop.

Use a scope to add fields to all logs in a block:

```csharp
using (logger.BeginScope(new Dictionary<string, object> { ["BookingId"] = id }))
{
    logger.LogInformation("Start the cancellation.");
    // All logs in this block have BookingId.
}
```

For the best speed, use source-generated logging:

```csharp
public static partial class Log
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Booking {BookingId} confirmed")]
    public static partial void BookingConfirmed(ILogger logger, Guid bookingId);
}
```

## Build

1. Move the connection string to user secrets.
2. Make a `BookingOptions` class with validation and `ValidateOnStart`.
3. Make an `AddApplication()` extension method. Move the registrations to it.
4. Add Serilog. Write the logs to the console as JSON.
5. Test the captive dependency error. Inject a scoped service into a singleton. See the error at start. Then correct it.

## Common mistakes

- You register a service as a singleton "for speed". Then it holds state between users.
- You inject `IConfiguration` in ten classes. Then a change of a key name breaks ten files.
- You log a password, a token, or personal data. Never do this.

## Check

- A background service must use a `DbContext`. How do you do it?
- Which options interface do you use for a value that can change at run time?

---

# Day 6 — The ASP.NET Core request pipeline

## Why this day

This is the shape of the whole web application. If the middleware order is wrong, the security is wrong.

## Concepts

### The pipeline

Each request goes down the middleware chain and then comes back up.

```csharp
app.UseExceptionHandler();      // 1. It must be first. It catches all below.
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();               // 2. It selects the endpoint.
app.UseCors();
app.UseAuthentication();        // 3. Who is the user?
app.UseAuthorization();         // 4. Can the user do this? It needs the endpoint from UseRouting.
app.UseRateLimiter();
app.MapControllers();           // 5. It runs the endpoint.
```

Learn this order. `UseAuthorization` after `UseRouting` and before the endpoint. If you put `UseAuthorization` after `MapControllers`, no authorization runs.

### Custom middleware

```csharp
public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var id = context.Request.Headers["X-Correlation-Id"].FirstOrDefault()
                 ?? Guid.CreateVersion7().ToString();
        context.Response.Headers["X-Correlation-Id"] = id;

        using (context.RequestServices.GetRequiredService<ILogger<CorrelationIdMiddleware>>()
                   .BeginScope(new Dictionary<string, object> { ["CorrelationId"] = id }))
        {
            await next(context);   // Call next. If you do not, the pipeline stops here.
        }
    }
}

app.UseMiddleware<CorrelationIdMiddleware>();
```

### Minimal APIs against controllers

Both are correct. Minimal APIs are the default choice for a new service now. They are faster and have less ceremony. Controllers are better if you need many filters, model binders, or OData.

**Minimal API with a route group:**

```csharp
public static class EventEndpoints
{
    public static void MapEventEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/events")
                       .WithTags("Events")
                       .RequireAuthorization();

        group.MapGet("/", GetAll);
        group.MapGet("/{id:guid}", GetById).WithName("GetEventById");
        group.MapPost("/", Create).AllowAnonymous();
    }

    private static async Task<Ok<IReadOnlyList<EventDto>>> GetAll(
        IEventService service, CancellationToken ct)
    {
        var items = await service.GetAllAsync(ct);
        return TypedResults.Ok(items);
    }

    private static async Task<Results<Ok<EventDto>, NotFound>> GetById(
        Guid id, IEventService service, CancellationToken ct)
    {
        var item = await service.GetAsync(id, ct);
        return item is null ? TypedResults.NotFound() : TypedResults.Ok(item);
    }
}

// Program.cs
app.MapEventEndpoints();
```

Use `TypedResults`, not `Results`. `TypedResults` gives the type to the OpenAPI document, and you can test it without a cast.

### Model binding

ASP.NET Core reads the parameters from these places:
- The route: `/events/{id}` → `Guid id`
- The query string: `?page=2` → `int page`
- The body (JSON): a complex type
- A service: any registered type
- Special types: `HttpContext`, `CancellationToken`, `ClaimsPrincipal`

Add an attribute if the source is not clear: `[FromQuery]`, `[FromBody]`, `[FromRoute]`, `[FromHeader]`.

Add a route constraint to reject bad data early: `{id:guid}`, `{page:int:min(1)}`.

### Validation

.NET 10 added validation to minimal APIs with data annotations. Turn it on:

```csharp
builder.Services.AddValidation();
```

For complex rules, use FluentValidation (Day 10). Data annotations are correct for simple shape checks.

### Errors: Problem Details

Return the standard error format (RFC 9457), not a custom shape.

```csharp
builder.Services.AddProblemDetails();

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context, Exception exception, CancellationToken ct)
    {
        logger.LogError(exception, "Unhandled exception");

        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "An unexpected error occurred.",
            Type = "https://tools.ietf.org/html/rfc9110#section-15.6.1"
        };

        context.Response.StatusCode = problem.Status.Value;
        await context.Response.WriteAsJsonAsync(problem, ct);
        return true;
    }
}

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
app.UseExceptionHandler();
```

Never send the exception message or the stack trace to the client in production.

### OpenAPI

Swashbuckle is not in the template now. .NET has built-in generation.

```csharp
builder.Services.AddOpenApi();
app.MapOpenApi();     // the JSON document at /openapi/v1.json
```

For a UI, add Scalar:

```csharp
app.MapScalarApiReference();   // package: Scalar.AspNetCore
```

### Versioning, CORS, and rate limiting

```csharp
// CORS
builder.Services.AddCors(o => o.AddPolicy("spa", p => p
    .WithOrigins("http://localhost:5173")
    .AllowAnyHeader()
    .AllowAnyMethod()));
app.UseCors("spa");

// Rate limiting
builder.Services.AddRateLimiter(o =>
{
    o.AddFixedWindowLimiter("fixed", opt =>
    {
        opt.PermitLimit = 100;
        opt.Window = TimeSpan.FromMinutes(1);
    });
});
app.UseRateLimiter();
```

Never use `AllowAnyOrigin` with `AllowCredentials`. The framework refuses it.

## Build

1. Make an `EventEndpoints` class with a route group. Move the endpoints to it.
2. Add the global exception handler with Problem Details.
3. Add a correlation identifier middleware.
4. Add rate limiting to the create endpoint.
5. Open the OpenAPI document. Confirm that the response types are correct.
6. Move `UseAuthorization` after `MapControllers`. See that the security stops working. Then move it back.

## Common mistakes

- You put `UseCors` after `UseRouting` but before `UseAuthorization`, and the preflight request fails. Read the documentation for the exact order.
- You return `Results.Ok(x)` and get no type in OpenAPI. Use `TypedResults`.
- You catch an exception in each endpoint. Use one global handler.

## Check

- What is the correct position of `UseAuthentication` and why?
- What is the difference between a 400 error and a 422 error?

---

# Day 7 — EF Core with PostgreSQL, part 1

## Why this day

EF Core is your data access. PostgreSQL has some differences from SQL Server. You must know both.

## Concepts

### Setup

```bash
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet tool install --global dotnet-ef
```

`docker-compose.yml`:

```yaml
services:
  postgres:
    image: postgres:17-alpine
    environment:
      POSTGRES_USER: eventhub
      POSTGRES_PASSWORD: local_dev_only
      POSTGRES_DB: eventhub
    ports:
      - "5432:5432"
    volumes:
      - pgdata:/var/lib/postgresql/data
volumes:
  pgdata:
```

### The DbContext

```csharp
public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Booking> Bookings => Set<Booking>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
```

The `DbSet<T> X => Set<T>()` form avoids a nullable warning. Use it.

### Entity configuration

Do not put attributes on the entity. Use a configuration class. The domain stays clean.

```csharp
public sealed class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.ToTable("events");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
               .HasConversion(id => id.Value, value => new EventId(value));

        builder.Property(e => e.Title)
               .HasMaxLength(200)
               .IsRequired();

        builder.Property(e => e.StartsAt)
               .HasColumnType("timestamptz");

        builder.HasIndex(e => e.StartsAt);

        builder.HasMany(e => e.Bookings)
               .WithOne(b => b.Event)
               .HasForeignKey(b => b.EventId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
```

### PostgreSQL specific points

- Use `timestamptz` for a time. Npgsql maps it to `DateTimeOffset` or to a UTC `DateTime`. Npgsql throws an exception if you save a `DateTime` with the kind `Local` or `Unspecified` to a `timestamptz` column. Always use UTC.
- The default naming is case sensitive. A column `Title` needs quotes in raw SQL. Use snake case names to avoid this. The `EFCore.NamingConventions` package does this automatically:

```csharp
options.UseNpgsql(connectionString).UseSnakeCaseNamingConvention();
```

- PostgreSQL has `jsonb`. EF Core can map a class or a list to it.
- PostgreSQL has real array columns: `string[]` maps to `text[]`.

### Value objects with owned types

```csharp
builder.OwnsOne(e => e.Price, price =>
{
    price.Property(p => p.Amount).HasColumnName("price_amount");
    price.Property(p => p.Currency).HasColumnName("price_currency").HasMaxLength(3);
});
```

The `Money` record has no table. Its fields go into the `events` table.

### Migrations

```bash
dotnet ef migrations add InitialCreate --project src/EventHub.Infrastructure --startup-project src/EventHub.Api
dotnet ef database update --project src/EventHub.Infrastructure --startup-project src/EventHub.Api
dotnet ef migrations remove       # remove the last one, if you did not apply it
dotnet ef migrations script       # make a SQL file for production
```

Rules:
- Always read the generated migration file before you apply it. EF Core sometimes drops a column that you want to rename.
- Never edit a migration after you push it to the team.
- Give a name that says what changes: `AddSeatCountToEvent`.

### The change tracker

```csharp
var e = await context.Events.FirstAsync(x => x.Id == id, ct);
e.Title = "New title";
await context.SaveChangesAsync(ct);   // EF Core sees the change. No Update call is necessary.
```

EF Core compares the current values with the values from the query. This is the change tracker.

For a read-only query, turn the tracker off. It is faster and it uses less memory:

```csharp
var list = await context.Events.AsNoTracking().ToListAsync(ct);
```

Rule: use `AsNoTracking()` for all read queries. Do not use it if you will change the data.

## Build

1. Start PostgreSQL with `docker compose up -d`.
2. Make the `AppDbContext` class with `Event` and `Booking`.
3. Write the configuration classes for both.
4. Add the snake case naming convention.
5. Make the first migration. Read the generated file. Apply it.
6. Connect with a client such as `psql`, DBeaver, or pgAdmin. Look at the tables.
7. Turn on the SQL log in Development:

```csharp
options.UseNpgsql(cs).EnableSensitiveDataLogging().LogTo(Console.WriteLine, LogLevel.Information);
```

8. Run one query. Read the SQL in the console.

## Common mistakes

- You save `DateTime.Now`. Always use `DateTime.UtcNow` or `DateTimeOffset.UtcNow`. Better: inject `TimeProvider` so that you can test the time.
- You add `EnableSensitiveDataLogging` in production. It writes the parameter values, and this can include personal data.
- You call `context.Update(entity)` on a tracked entity. This marks all columns as changed.

## Check

- Where does EF Core store the list of applied migrations?
- What is the difference between `Include` and a projection with `Select`?

---

# Day 8 — EF Core with PostgreSQL, part 2

## Why this day

Part 1 makes the data work. Part 2 makes it correct and fast.

## Concepts

### The N+1 problem

```csharp
var events = await context.Events.ToListAsync(ct);      // 1 query
foreach (var e in events)
{
    var count = e.Bookings.Count;   // 1 query for each event. This is N more queries.
}
```

Three solutions:

```csharp
// 1. Include. It joins. It can return much duplicate data.
await context.Events.Include(e => e.Bookings).ToListAsync(ct);

// 2. Split query. It sends 2 queries. It is better for a one-to-many with much data.
await context.Events.Include(e => e.Bookings).AsSplitQuery().ToListAsync(ct);

// 3. Projection. This is the best for a read endpoint.
await context.Events
    .Select(e => new EventListDto(e.Id.Value, e.Title, e.Bookings.Count))
    .ToListAsync(ct);
```

A projection reads only the columns that you need. It does not need `AsNoTracking`, because there is no entity to track.

**Turn on the warning.** EF Core can throw an exception when a lazy load happens:

```csharp
options.ConfigureWarnings(w => w.Throw(CoreEventId.LazyLoadOnDisposedContextWarning));
```

### Transactions

`SaveChangesAsync` is one transaction. All changes succeed, or all fail.

You need an explicit transaction only for many `SaveChanges` calls:

```csharp
await using var transaction = await context.Database.BeginTransactionAsync(ct);
try
{
    await context.SaveChangesAsync(ct);
    await otherService.DoWorkAsync(ct);
    await transaction.CommitAsync(ct);
}
catch
{
    await transaction.RollbackAsync(ct);
    throw;
}
```

### Concurrency

Two users book the last seat at the same time. Both read `AvailableSeats = 1`. Both write. The system sells two seats.

Optimistic concurrency stops this. PostgreSQL has a hidden `xmin` system column. Use it:

```csharp
builder.UseXminAsConcurrencyToken();
```

Now the `UPDATE` statement includes `WHERE xmin = @original`. If another user changed the row, zero rows are affected, and EF Core throws `DbUpdateConcurrencyException`.

```csharp
try
{
    await context.SaveChangesAsync(ct);
}
catch (DbUpdateConcurrencyException)
{
    return Result.Conflict("Another user changed this event. Try again.");
}
```

For a very high load, use a database-level guard instead:

```csharp
var rows = await context.Events
    .Where(e => e.Id == id && e.TotalSeats - e.BookedSeats >= seats)
    .ExecuteUpdateAsync(s => s.SetProperty(e => e.BookedSeats, e => e.BookedSeats + seats), ct);

if (rows == 0) { /* no seats */ }
```

### Bulk operations

These run one SQL statement. They do not load the entities. They do not use the change tracker.

```csharp
await context.Bookings
    .Where(b => b.CreatedAt < cutoff && b.Status == BookingStatus.Pending)
    .ExecuteDeleteAsync(ct);

await context.Events
    .Where(e => e.StartsAt < now)
    .ExecuteUpdateAsync(s => s.SetProperty(e => e.IsClosed, true), ct);
```

Warning: they do not raise domain events, and they do not update the tracked entities in memory.

### Interceptors for audit fields

```csharp
public sealed class AuditInterceptor(TimeProvider timeProvider) : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result,
        CancellationToken ct = default)
    {
        var context = eventData.Context;
        if (context is not null)
        {
            var now = timeProvider.GetUtcNow();
            foreach (var entry in context.ChangeTracker.Entries<IAuditable>())
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.CreatedAtUtc = now;
                }
                if (entry.State is EntityState.Added or EntityState.Modified)
                {
                    entry.Entity.UpdatedAtUtc = now;
                }
            }
        }
        return base.SavingChangesAsync(eventData, result, ct);
    }
}
```

Register it:

```csharp
options.UseNpgsql(cs).AddInterceptors(new AuditInterceptor(TimeProvider.System));
```

### Indexes and constraints

Put the rules in the database, not only in the code.

```csharp
builder.HasIndex(e => e.Title).IsUnique();
builder.HasIndex(b => new { b.EventId, b.UserId }).IsUnique();
builder.ToTable(t => t.HasCheckConstraint("ck_events_seats", "\"total_seats\" > 0"));
```

An index on a foreign key is usually automatic. An index for a filter or a sort is your work.

### Raw SQL

Use it when LINQ cannot do the work. It is safe if you use interpolation with `FromSql`:

```csharp
var events = await context.Events
    .FromSql($"SELECT * FROM events WHERE title ILIKE {pattern}")
    .ToListAsync(ct);
```

`FromSql` makes parameters. `FromSqlRaw` does not. Never build a string with user input.

### Pooling

A `DbContext` is cheap, but not free. Use pooling in a high-load API:

```csharp
builder.Services.AddDbContextPool<AppDbContext>(options => ...);
```

Do not use pooling if your context holds state in fields.

## Build

1. Add `UseXminAsConcurrencyToken` to the `Event` entity.
2. Write a test: two parallel bookings for the last seat. Confirm that one fails.
3. Add the audit interceptor with `TimeProvider`.
4. Change one endpoint from `Include` to a projection. Compare the SQL.
5. Add a unique index. See that the second insert fails with a database error. Catch it and return a clear message.
6. Add a background clean-up with `ExecuteDeleteAsync`.

## Common mistakes

- You catch `DbUpdateException` and show the raw database message to the user.
- You add an index for each column "for speed". An index makes writes slower. Add an index for a real query.
- You call `SaveChangesAsync` inside a loop. Call it one time after the loop.

## Check

- Explain optimistic concurrency to a person who does not know it.
- When does `ExecuteUpdateAsync` give the wrong result in your application logic?

---

# Day 9 — Architecture and the folder structure

## Why this day

You now know the parts. Today you put them in the correct place. The compiler helps you keep the rules.

## Concepts

### The three common styles

**1. Layered (N-tier).** Controller → Service → Repository → Database. It is simple. It becomes a big "service" layer with no shape.

**2. Clean Architecture (also called Onion or Hexagonal).** The dependencies point inward. The domain knows nothing about the database or the web.

**3. Vertical Slice.** One folder for each feature. Each folder holds the request, the handler, and the response. It is very good for a team that adds features.

Clean Architecture and Vertical Slice work together. Use Clean Architecture for the project boundaries, and vertical slices inside the Application project.

### The dependency rule

```
Api  →  Infrastructure  →  Application  →  Domain
                              ↑                ↑
                              └──── Domain ────┘
```

- `Domain` references nothing. No NuGet packages. No EF Core.
- `Application` references `Domain` only. It holds the use cases and the interfaces.
- `Infrastructure` references `Application`. It gives the real classes for the interfaces.
- `Api` references `Infrastructure` and `Application`. It wires everything at start.

**Why does `Domain` have no packages?** Because business rules must not change when you change the database or the web framework. You can test them with no setup.

### The dependency inversion

`Application` needs to save data. But it cannot reference EF Core. So:

```csharp
// in Application
public interface IEventRepository
{
    Task<Event?> GetAsync(EventId id, CancellationToken ct);
    Task AddAsync(Event item, CancellationToken ct);
}

// in Infrastructure
internal sealed class EventRepository(AppDbContext context) : IEventRepository
{
    public Task<Event?> GetAsync(EventId id, CancellationToken ct) =>
        context.Events.FirstOrDefaultAsync(e => e.Id == id, ct);

    public Task AddAsync(Event item, CancellationToken ct) =>
        context.Events.AddAsync(item, ct).AsTask();
}
```

The interface is in the inner layer. The class is in the outer layer. This is dependency inversion.

### Do you need a repository?

This is a real debate. Know both sides.

**Against:** `DbContext` is already a unit of work, and `DbSet<T>` is already a repository. A repository that wraps it adds code and hides the power of LINQ.

**For:** It keeps EF Core out of the `Application` project. It makes unit tests easy.

**A practical answer:**
- For **writes**, use a repository for each aggregate. The methods are few: `GetAsync`, `Add`, `Remove`. This protects the business rules.
- For **reads**, do not use a repository. Query the `DbContext` directly in a query handler in the `Infrastructure` project, or use Dapper. Reads need speed and shape, not abstraction.

This is CQRS in a light form. You learn it on Day 10.

### The Result pattern

Do not use an exception for a business rule. An exception is slow and it hides the flow.

```csharp
public sealed record Error(string Code, string Message);

public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public Error? Error { get; }

    private Result(T value) { IsSuccess = true; Value = value; }
    private Result(Error error) { IsSuccess = false; Error = error; }

    public static Result<T> Success(T value) => new(value);
    public static Result<T> Failure(Error error) => new(error);
}
```

Use it:

```csharp
if (evt.AvailableSeats < request.Seats)
{
    return Result<BookingDto>.Failure(new Error("booking.no_seats", "Not enough seats."));
}
```

Then the endpoint maps the error to an HTTP status code. Use an exception only for a true fault: a lost database connection, a bug, or bad configuration.

You can also use a library such as `ErrorOr` or `FluentResults`.

### DTOs and mapping

Never send an entity to the client. Reasons:
- The entity has fields that the client must not see.
- A change in the database then breaks your API contract.
- A navigation property causes a circular reference in JSON.

Map by hand:

```csharp
public static EventDto ToDto(this Event e) =>
    new(e.Id.Value, e.Title, e.StartsAt, e.AvailableSeats);
```

AutoMapper is popular, but it moves errors from build time to run time. It became a paid product for commercial use in 2025. Manual mapping, or a source generator such as Mapperly, is better now.

## Build

Move to this structure.

```
eventhub/
├─ Directory.Build.props
├─ Directory.Packages.props
├─ EventHub.sln
├─ docker-compose.yml
├─ src/
│  ├─ EventHub.Domain/
│  │  ├─ Common/            (Entity, AggregateRoot, IDomainEvent)
│  │  ├─ Events/            (Event.cs, EventId.cs, EventErrors.cs)
│  │  └─ Bookings/          (Booking.cs, BookingStatus.cs)
│  ├─ EventHub.Application/
│  │  ├─ Common/            (Result, IUnitOfWork, behaviours)
│  │  ├─ Events/
│  │  │  ├─ CreateEvent/    (Command, Handler, Validator, Response)
│  │  │  └─ GetEventById/
│  │  └─ Bookings/
│  │     └─ CreateBooking/
│  ├─ EventHub.Infrastructure/
│  │  ├─ Persistence/       (AppDbContext, Configurations, Migrations, Repositories)
│  │  ├─ Services/          (email, clock, external calls)
│  │  └─ DependencyInjection.cs
│  └─ EventHub.Api/
│     ├─ Endpoints/
│     ├─ Middleware/
│     ├─ Extensions/
│     └─ Program.cs
└─ tests/
   ├─ EventHub.Domain.UnitTests/
   ├─ EventHub.Application.UnitTests/
   └─ EventHub.Api.IntegrationTests/
```

Steps:
1. Make the four projects with `dotnet new classlib` and add them to the solution.
2. Add the project references in the correct direction only.
3. Move the entities to `Domain`. Delete every `using Microsoft.EntityFrameworkCore;` from them.
4. Move the `DbContext` and the configurations to `Infrastructure`.
5. Make the repository interfaces in `Application`. Make the classes in `Infrastructure`.
6. Make the `Result` type.
7. Correct all build errors. **Each error teaches you the rule.**
8. Add an architecture test with `NetArchTest.Rules`:

```csharp
[Fact]
public void Domain_should_not_depend_on_other_projects()
{
    var result = Types.InAssembly(typeof(Event).Assembly)
        .ShouldNot()
        .HaveDependencyOnAny("EventHub.Application", "EventHub.Infrastructure", "Microsoft.EntityFrameworkCore")
        .GetResult();

    result.IsSuccessful.Should().BeTrue();
}
```

## Common mistakes

- You put the DTOs in `Api`. Then `Application` cannot return them. Put the DTOs in `Application`.
- You add a reference from `Domain` to `Application` "for one small thing". This breaks the whole design. Find another way.
- You make one interface for each class. This adds no value. Make an interface when you must cross a layer boundary or must replace the class in a test.

## Check

- Where do you put an interface for an email service? Where does the class go?
- Why must the `Domain` project have no NuGet packages?

---

# Day 10 — Use cases, validation, and cross-cutting behaviour

## Why this day

Your `Application` layer needs a shape. If not, it becomes ten large service classes with 30 methods each.

## Concepts

### CQRS in a light form

CQRS separates the **command** (change the data) from the **query** (read the data).

- A **command** goes through the domain model. It uses the repository. It has business rules.
- A **query** goes directly to the database. It returns a DTO. It has no domain model.

This is not two databases. This is two code paths in the same application.

### The mediator pattern

A mediator sends a request to its handler. The caller does not know the handler.

MediatR was the standard package. It became a paid product for commercial use in 2025. Your options:
- Write a small mediator. It is about 40 lines. This is a good exercise.
- Use a free alternative such as Wolverine.
- Use MediatR with a licence if your company pays.

A simple mediator:

```csharp
public interface IRequest<TResponse>;

public interface IRequestHandler<TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    Task<TResponse> HandleAsync(TRequest request, CancellationToken ct);
}

public interface ISender
{
    Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken ct);
}
```

Do you need a mediator at all? No. You can inject the handler directly:

```csharp
group.MapPost("/", async (CreateEventCommand cmd, CreateEventHandler handler, CancellationToken ct)
    => await handler.HandleAsync(cmd, ct));
```

This is simple and clear. The mediator gives you one thing: a pipeline for cross-cutting work. If you do not need the pipeline, do not add the mediator.

### One folder for each use case

```
Application/Events/CreateEvent/
├─ CreateEventCommand.cs
├─ CreateEventHandler.cs
├─ CreateEventValidator.cs
└─ CreateEventResponse.cs
```

Everything for one feature is in one folder. To delete the feature, delete the folder.

```csharp
public sealed record CreateEventCommand(
    string Title,
    DateTimeOffset StartsAt,
    int TotalSeats) : IRequest<Result<Guid>>;

public sealed class CreateEventHandler(
    IEventRepository repository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : IRequestHandler<CreateEventCommand, Result<Guid>>
{
    public async Task<Result<Guid>> HandleAsync(CreateEventCommand request, CancellationToken ct)
    {
        if (request.StartsAt <= timeProvider.GetUtcNow())
        {
            return Result<Guid>.Failure(EventErrors.StartsInThePast);
        }

        var item = Event.Create(request.Title, request.StartsAt, request.TotalSeats);

        await repository.AddAsync(item, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Result<Guid>.Success(item.Id.Value);
    }
}
```

Note: the handler is thin. The rules are in the `Event.Create` factory method in the domain.

### FluentValidation

Data annotations check the shape. FluentValidation checks the rules.

```csharp
public sealed class CreateEventValidator : AbstractValidator<CreateEventCommand>
{
    public CreateEventValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.TotalSeats)
            .GreaterThan(0)
            .LessThanOrEqualTo(10_000);

        RuleFor(x => x.StartsAt)
            .Must(d => d > DateTimeOffset.UtcNow)
            .WithMessage("The event must start in the future.");
    }
}
```

Register all validators:

```csharp
services.AddValidatorsFromAssembly(typeof(CreateEventValidator).Assembly);
```

### Pipeline behaviour

A behaviour runs around each handler. It is middleware for your use cases.

```csharp
public sealed class ValidationBehaviour<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators)
{
    public async Task<TResponse> HandleAsync(
        TRequest request, Func<Task<TResponse>> next, CancellationToken ct)
    {
        var failures = validators
            .Select(v => v.Validate(request))
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count > 0)
        {
            throw new ValidationException(failures);
        }

        return await next();
    }
}
```

Common behaviours:
- **Validation** — check the request before the handler.
- **Logging** — log the start, the end, and the duration.
- **Transaction** — open a transaction for a command, and commit it after.
- **Performance** — warn if a handler takes more than 500 ms.
- **Caching** — return a cached result for a query.

Why is this better than a base class? Because you can add or remove one behaviour with one line, and it applies to all handlers. Inheritance cannot do this.

### Domain events

The domain raises an event. Another part reacts. The domain does not know who reacts.

```csharp
// in the domain
public sealed class Booking : AggregateRoot
{
    public static Booking Create(Event evt, Guid userId, int seats)
    {
        var booking = new Booking { /* ... */ };
        booking.Raise(new BookingCreatedDomainEvent(booking.Id, evt.Id, userId));
        return booking;
    }
}
```

Publish them after `SaveChangesAsync`:

```csharp
public async Task<int> SaveChangesAsync(CancellationToken ct)
{
    var events = ChangeTracker.Entries<AggregateRoot>()
        .SelectMany(e => e.Entity.DomainEvents)
        .ToList();

    var result = await base.SaveChangesAsync(ct);

    foreach (var domainEvent in events)
    {
        await publisher.PublishAsync(domainEvent, ct);
    }

    return result;
}
```

Warning: if the publish fails, the data is saved but the reaction did not happen. Day 13 solves this with the outbox pattern.

## Build

1. Make one folder for each use case: `CreateEvent`, `GetEventById`, `ListEvents`, `CreateBooking`, `CancelBooking`.
2. Write the command, the handler, and the validator for each.
3. Add a validation behaviour and a logging behaviour.
4. Move the seat rules into the `Event` and `Booking` domain classes. The handler must only orchestrate.
5. Raise a `BookingCreatedDomainEvent` event. Handle it. Write a log line.
6. Make the endpoints thin. One line for each.

## Common mistakes

- Your handler has 200 lines of business logic. Move the logic to the domain.
- You validate in the controller, in the handler, and in the domain. Choose the correct place for each check: shape in the validator, business rules in the domain.
- You inject ten dependencies into one handler. This is a signal. Divide the use case.

## Check

- What is the difference between a domain event and an integration event?
- Where do you check "the title must be unique"? The validator or the domain? Why?

---

# Day 11 — Security and testing

## Why this day

You must not deploy without these two. This day closes Stage 1.

## Concepts — security

### JWT authentication

A JSON Web Token has three parts: a header, a payload with claims, and a signature. The server does not store it. The server checks the signature.

```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtOptions.Secret)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });
```

Set all five `Validate` flags to `true`. The default `ClockSkew` is 5 minutes. Make it small.

### Access token and refresh token

- **Access token** — short life (10 to 15 minutes). It is sent with each request. It cannot be cancelled.
- **Refresh token** — long life (7 to 30 days). It is stored in the database. It can be cancelled. Use it one time only, and then give a new one (rotation).

Store the refresh token as a hash, not as plain text.

### Authorization with policies

Do not test a role in the code. Test a policy.

```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CanManageEvents", policy =>
        policy.RequireRole("Organizer", "Admin"));

    options.AddPolicy("AtLeast18", policy =>
        policy.RequireAssertion(ctx =>
        {
            var claim = ctx.User.FindFirst("date_of_birth")?.Value;
            return DateOnly.TryParse(claim, out var dob)
                && dob.AddYears(18) <= DateOnly.FromDateTime(DateTime.UtcNow);
        }));
});

group.MapPost("/", Create).RequireAuthorization("CanManageEvents");
```

Why a policy? Because a change of the rule is one line in one file. A role check spread across 40 endpoints is not maintainable.

For a rule about one item ("can this user cancel this booking?"), use resource-based authorization with `IAuthorizationHandler`.

### Passwords and identity

ASP.NET Core Identity gives users, passwords, roles, and tokens. It works with EF Core. .NET 10 added passkey support.

If you do not use Identity, hash the password with Argon2 or with `PasswordHasher<T>` from Identity. Never use MD5 or SHA-256 alone for a password.

### The security list

- Turn on HTTPS. Add HSTS in production.
- Never log a token, a password, or personal data.
- Give the least privilege to the database user.
- Validate all input. Never build SQL with a string.
- Do not send the exception details to the client.
- Set the CORS origins to a fixed list.
- Add rate limiting to the login endpoint.
- Keep the packages new. Run `dotnet list package --vulnerable`.

## Concepts — testing

### The three types

| Type | What it tests | Speed | How many |
|---|---|---|---|
| Unit | One class. No input or output. | Very fast | Many |
| Integration | Many parts with a real database. | Slow | Some |
| End to end | The full system through HTTP. | Very slow | Few |

Test the domain rules with unit tests. Test the endpoints with integration tests.

### Unit test

```csharp
public class EventTests
{
    [Fact]
    public void Book_should_fail_when_not_enough_seats()
    {
        var evt = Event.Create("Meetup", DateTimeOffset.UtcNow.AddDays(1), totalSeats: 2);

        var result = evt.Book(seats: 3);

        result.IsSuccess.ShouldBeFalse();
        result.Error!.Code.ShouldBe("event.not_enough_seats");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_should_fail_for_invalid_seat_count(int seats)
    {
        var act = () => Event.Create("Meetup", DateTimeOffset.UtcNow.AddDays(1), seats);
        Should.Throw<ArgumentException>(act);
    }
}
```

- `[Fact]` — one test with no parameters.
- `[Theory]` with `[InlineData]` — the same test with different data.

Note on assertions: FluentAssertions version 8 needs a paid licence for commercial use. Use Shouldly or AwesomeAssertions. The xUnit `Assert` class is also correct.

### Test doubles

Use NSubstitute or Moq for the interfaces.

```csharp
var repository = Substitute.For<IEventRepository>();
repository.GetAsync(Arg.Any<EventId>(), Arg.Any<CancellationToken>())
          .Returns(existingEvent);

var handler = new CreateBookingHandler(repository, unitOfWork, TimeProvider.System);
```

Use a fake time to test the time rules:

```csharp
var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
// package: Microsoft.Extensions.TimeProvider.Testing
```

This is a good reason to inject `TimeProvider` and to never call `DateTime.UtcNow` directly.

### Integration test with a real database

Do not use the in-memory provider. It is not PostgreSQL. It does not have constraints, transactions, or SQL. A test that passes there can fail in production. Use Testcontainers.

```csharp
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _db = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.AddDbContext<AppDbContext>(o => o.UseNpgsql(_db.GetConnectionString()));
        });
    }

    public async Task InitializeAsync()
    {
        await _db.StartAsync();
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Database.MigrateAsync();
    }

    public new async Task DisposeAsync() => await _db.DisposeAsync();
}
```

The test:

```csharp
public class EventEndpointTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Create_event_returns_201()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/events", new
        {
            title = "Meetup",
            startsAt = DateTimeOffset.UtcNow.AddDays(7),
            totalSeats = 50
        });

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }
}
```

Note: `WebApplicationFactory<Program>` needs the `Program` class to be visible. With top-level statements, add this line at the end of `Program.cs`:

```csharp
public partial class Program;
```

### What to test

- Test the business rules. Test the edge cases. Test the failures.
- Do not test the framework. Do not test a property setter.
- Do not aim for 100% coverage. Aim for confidence.

## Build

1. Add JWT authentication and a login endpoint that gives a token.
2. Add a `CanManageEvents` policy. Protect the create endpoint.
3. Add refresh tokens with rotation.
4. Write 10 unit tests for the domain rules.
5. Write 5 integration tests with Testcontainers.
6. Add `dotnet test` to your Git pre-push routine.
7. Run `dotnet list package --vulnerable --include-transitive`.

## Common mistakes

- You set `ValidateAudience` to `false` to make it work. Now a token from another system is accepted.
- You use the in-memory EF Core provider and believe that the tests prove the SQL.
- Your test depends on the test before it. Each test must be independent.

## Check

- Why must an access token be short?
- Why is a test against a real PostgreSQL container better than one with the in-memory provider?

---

# The Stage 1 review

At the end of Day 11, you must have this:

- A Web API with four projects and a correct dependency direction.
- PostgreSQL in Docker with EF Core migrations.
- One folder for each use case, with a validator and a handler.
- JWT authentication and policy-based authorization.
- A global exception handler with Problem Details.
- Unit tests and integration tests that run with one command.
- Structured logs and a health endpoint.
- No warnings in the build.

Now you can go to Stage 2.
