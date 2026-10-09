using Lab0.Abstractions;
using Lab0.Domain;
using Lab0.Infrastructure;
using Lab0.Scoring;
using Lab0.Services;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);
var localEnvironment = LocalEnvironmentFile.Read();
var connectionString = builder.Configuration.GetConnectionString("Watchlist")
    ?? localEnvironment.GetValueOrDefault("ConnectionStrings__Watchlist")
    ?? CreateConnectionString(builder.Configuration, localEnvironment)
    ?? throw new InvalidOperationException(
        "PostgreSQL is not configured. Create Lab0/.env from .env.example, set the database password, " +
        "and restart the app. Rider run-configuration environment variables are also supported.");

builder.WebHost.UseUrls("http://127.0.0.1:5080");
builder.Services.AddSingleton(NpgsqlDataSource.Create(connectionString));
builder.Services.AddSingleton<IWatchlistRepository, PostgresWatchlistRepository>();
builder.Services.AddSingleton<IMediaScoreCalculator, RatingScoreCalculator>();
builder.Services.AddScoped<IWatchlistService, WatchlistService>();

var app = builder.Build();

try
{
    await app.Services.GetRequiredService<IWatchlistRepository>().InitializeAsync();
}
catch (NpgsqlException exception)
{
    app.Logger.LogCritical(exception, "Could not initialize the PostgreSQL watchlist database.");
    throw;
}

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/health", async (IWatchlistRepository repository, CancellationToken cancellationToken) =>
{
    await repository.GetWatchlistAsync(cancellationToken);
    return Results.Ok(new { status = "ok" });
});

app.MapGet("/api/watchlist", async (IWatchlistService service, CancellationToken cancellationToken) =>
    Results.Ok(await service.GetWatchlistAsync(cancellationToken)));

app.MapPost("/api/categories", async (
    CreateNamedItemRequest request,
    IWatchlistService service,
    CancellationToken cancellationToken) =>
{
    await service.CreateCategoryAsync(request.Name, cancellationToken);
    return Results.NoContent();
});

app.MapPost("/api/tags", async (
    CreateNamedItemRequest request,
    IWatchlistService service,
    CancellationToken cancellationToken) =>
{
    await service.CreateTagAsync(request.Name, cancellationToken);
    return Results.NoContent();
});

app.MapPost("/api/media", async (
    MediaItemRequest request,
    IWatchlistService service,
    CancellationToken cancellationToken) =>
{
    var item = await service.AddMediaItemAsync(request, cancellationToken);
    return Results.Created($"/api/media/{item.Id}", item);
});

app.MapPut("/api/media/{id:int}", async (
    int id,
    MediaItemRequest request,
    IWatchlistService service,
    CancellationToken cancellationToken) =>
{
    var item = await service.UpdateMediaItemAsync(id, request, cancellationToken);
    return Results.Ok(item);
});

app.MapPost("/api/media/{id:int}/consumed", async (
    int id,
    IWatchlistService service,
    CancellationToken cancellationToken) =>
{
    await service.MarkAsConsumedAsync(id, cancellationToken);
    return Results.NoContent();
});

app.MapDelete("/api/media/{id:int}", async (
    int id,
    IWatchlistService service,
    CancellationToken cancellationToken) =>
{
    await service.DeleteMediaItemAsync(id, cancellationToken);
    return Results.NoContent();
});

app.MapPut("/api/media/{id:int}/rating", async (
    int id,
    RatingRequest request,
    IWatchlistService service,
    CancellationToken cancellationToken) =>
{
    await service.RateMediaItemAsync(id, request.Rating, cancellationToken);
    return Results.NoContent();
});

app.MapPost("/api/custom-fields", async (
    CustomFieldRequest request,
    IWatchlistService service,
    CancellationToken cancellationToken) =>
{
    var field = await service.CreateCustomFieldAsync(request, cancellationToken);
    return Results.Created($"/api/custom-fields/{field.Id}", field);
});

app.MapPut("/api/custom-fields/{id:int}", async (
    int id,
    CustomFieldRequest request,
    IWatchlistService service,
    CancellationToken cancellationToken) =>
    Results.Ok(await service.UpdateCustomFieldAsync(id, request, cancellationToken)));

app.MapDelete("/api/custom-fields/{id:int}", async (
    int id,
    IWatchlistService service,
    CancellationToken cancellationToken) =>
{
    await service.DeleteCustomFieldAsync(id, cancellationToken);
    return Results.NoContent();
});

app.Use(async (context, next) =>
{
    try
    {
        await next(context);
    }
    catch (ArgumentException exception)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsJsonAsync(new { error = exception.Message });
    }
    catch (KeyNotFoundException exception)
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        await context.Response.WriteAsJsonAsync(new { error = exception.Message });
    }
    catch (InvalidOperationException exception)
    {
        context.Response.StatusCode = StatusCodes.Status409Conflict;
        await context.Response.WriteAsJsonAsync(new { error = exception.Message });
    }
    catch (NpgsqlException exception)
    {
        app.Logger.LogError(exception, "A PostgreSQL operation failed.");
        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        await context.Response.WriteAsJsonAsync(new
        {
            error = "The database operation failed. Check the PostgreSQL service and application logs."
        });
    }
});

app.Run();

static string? CreateConnectionString(
    IConfiguration configuration,
    IReadOnlyDictionary<string, string> localEnvironment)
{
    var database = configuration["POSTGRES_DB"] ?? localEnvironment.GetValueOrDefault("POSTGRES_DB");
    var username = configuration["POSTGRES_USER"] ?? localEnvironment.GetValueOrDefault("POSTGRES_USER");
    var password = configuration["POSTGRES_PASSWORD"] ?? localEnvironment.GetValueOrDefault("POSTGRES_PASSWORD");

    if (string.IsNullOrWhiteSpace(database) ||
        string.IsNullOrWhiteSpace(username) ||
        string.IsNullOrWhiteSpace(password))
    {
        return null;
    }

    return new NpgsqlConnectionStringBuilder
    {
        Host = configuration["WATCHLIST_DB_HOST"] ?? localEnvironment.GetValueOrDefault("WATCHLIST_DB_HOST") ?? "127.0.0.1",
        Port = int.TryParse(
            configuration["WATCHLIST_DB_PORT"] ?? localEnvironment.GetValueOrDefault("WATCHLIST_DB_PORT"),
            out var port) ? port : 5432,
        Database = database,
        Username = username,
        Password = password
    }.ConnectionString;
}
