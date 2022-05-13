using Microsoft.Extensions.Caching.Memory;

const string cookieName = "qq";
TimeSpan idleTimeout = TimeSpan.FromSeconds(20);

int cookieSequenceGen = 0;

var entryOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(idleTimeout);
PostEvictionCallbackRegistration postEvictionCallbackRegistration = new PostEvictionCallbackRegistration();
postEvictionCallbackRegistration.State = typeof(Program);
postEvictionCallbackRegistration.EvictionCallback = (k, v, r, s) =>
{
    if (r is EvictionReason.Expired && s == typeof(Program) && v is IDisposable disposable)
    {
        disposable.Dispose();
    }
};
entryOptions.PostEvictionCallbacks.Add(postEvictionCallbackRegistration);

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMemoryCache(op =>
{
    op.ExpirationScanFrequency = TimeSpan.FromSeconds(1);
});

builder.Logging.ClearProviders();
builder.Logging.AddConsole(op =>
{
    op.TimestampFormat = "[HH:mm:ss:fff] ";
});

//builder.Services.AddScoped<SessionHolder>();
//builder.Services.AddScoped<Session>(op => op.GetRequiredService<SessionHolder>().session);

var app = builder.Build();

app.Use(async (context, next) =>
{
    IMemoryCache sessions = context.RequestServices.GetRequiredService<IMemoryCache>();
    Session session = null;
    int caseMatch = 0;
    string key = context.Request.Cookies[cookieName];
    bool isNewSession = false;
    if (
        key is null
        || !sessions.TryGetValue(key, out object sessionObj)
        || (session = sessionObj as Session) is null
    )
    {
        key = $"{Guid.NewGuid()}:{Interlocked.Increment(ref cookieSequenceGen)}";
        session = new(context.RequestServices.GetRequiredService<ILogger<Session>>());
        context.Response.Cookies.Append(cookieName, key);
        isNewSession = true;
    }
    ILogger<Program> logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    logger.LogInformation($"{context.Connection.Id}: {context.Request.Path}: {session}({session.GetHashCode()})");
    //context.RequestServices.GetRequiredService<SessionHolder>().session = session;
    try
    {
        await next?.Invoke();
        if (isNewSession)
        {
            sessions.Set(key, session, entryOptions);
        }
    }
    catch (Exception ex)
    {
        throw;
    }
});

app.MapGet("/", async context =>
{
    //await context.Response.WriteAsync($"{context.RequestServices.GetRequiredService<Session>()}({context.RequestServices.GetRequiredService<Session>().GetHashCode()})");
    await context.Response.WriteAsync($"Hello, World!");
});

app.Run();

public class Session : IDisposable
{
    private readonly ILogger<Session> _logger;

    public Session(ILogger<Session> logger) => _logger = logger;
    public void Dispose()
    {
        _logger.LogInformation($"{this}({GetHashCode()}) disposed");
    }
}

public class SessionHolder
{
    public Session session;
}