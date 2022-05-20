using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;

const string cookieName = "qq";
TimeSpan idleTimeout = TimeSpan.FromSeconds(20);

int cookieSequenceGen = 0;

var entryOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(idleTimeout);
PostEvictionCallbackRegistration postEvictionCallbackRegistration = new PostEvictionCallbackRegistration();
postEvictionCallbackRegistration.State = typeof(Program);
postEvictionCallbackRegistration.EvictionCallback = (k, v, r, s) =>
{
    if (r is EvictionReason.Expired && s is Type sType && sType == typeof(Program) && v is IDisposable disposable)
    {
        disposable.Dispose();
    }
};
entryOptions.PostEvictionCallbacks.Add(postEvictionCallbackRegistration);

System.Timers.Timer checkSessions = null!;
TimeSpan checkSessionsInterval = TimeSpan.FromSeconds(1);

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMemoryCache(op =>
{
    op.ExpirationScanFrequency = TimeSpan.FromSeconds(1);
});

builder.Services.AddScoped<SessionHolder>();
builder.Services.AddScoped<InfoProvider>();
builder.Services.AddScoped<Another>();

builder.Logging.ClearProviders();
builder.Logging.AddConsole(op =>
{
    op.TimestampFormat = "[HH:mm:ss:fff] ";
});

var app = builder.Build();

app.Use(async (context, next) =>
{
    IMemoryCache sessions = context.RequestServices.GetRequiredService<IMemoryCache>();
    if (checkSessions is null)
    {
        lock (app)
        {
            if (checkSessions is null)
            {
                checkSessions = new(checkSessionsInterval.TotalMilliseconds);
                checkSessions.Elapsed += (s, e) =>
                {
                    sessions.TryGetValue(string.Empty, out object dumb);
                };
                checkSessions.Enabled = true;
                checkSessions.AutoReset = true;
            }
        }
    }
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
        session = new(context.RequestServices.CreateScope().ServiceProvider);
        #region добавлено
        session.SessionServiceProvider.GetRequiredService<SessionHolder>().Session = session;
        #endregion добавлено
        context.Response.Cookies.Append(cookieName, key);
        isNewSession = true;
    }
    #region добавлено
    session.RequestServiceProvider = context.RequestServices;
    #endregion добавлено
    context.RequestServices.GetRequiredService<SessionHolder>().Session = session;

    ILogger<Program> logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    logger.LogInformation($"{context.Connection.Id}: {context.Request.Path}: {session}({session.GetHashCode()})");

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
    Session session = context.RequestServices.GetRequiredService<SessionHolder>().Session;
    Another another = context.RequestServices.GetRequiredService<Another>();
    await context.Response.WriteAsync($"[{DateTime.Now.ToString("HH:mm:ss.fff")}] Hello, World! {session}({session.GetHashCode()})"
        + $", controller {another}({another.GetHashCode()}), "
        + session.SessionServiceProvider.GetRequiredService<InfoProvider>().Get());
});

app.Run();

public class Session : IDisposable
{
    private readonly ILogger<Session> _logger;
    public IServiceProvider SessionServiceProvider { get; init; }
    #region добавлено
    public IServiceProvider RequestServiceProvider { get; set; }
    #endregion добавлено

    public Session(IServiceProvider serviceProvider) =>
        (SessionServiceProvider, _logger) = (serviceProvider, serviceProvider.GetRequiredService<ILogger<Session>>());
    public void Dispose()
    {
        if (SessionServiceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
        _logger.LogInformation($"{this}({GetHashCode()}) disposed");
    }
}

public class SessionHolder
{
    public Session Session { get; set; }
}

public class InfoProvider : IDisposable
{
    private ConcurrentQueue<int> _queue = new();
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private Task _fill = null;
    private readonly ILogger<InfoProvider> _logger;
    private readonly IServiceProvider _serviceProvider;

    public InfoProvider(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _logger = _serviceProvider.GetRequiredService<ILogger<InfoProvider>>();
        int value = 0;
        CancellationToken cancellationToken = _cancellationTokenSource.Token;
        _fill = Task.Run(async () =>
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(1000);
                _queue.Enqueue(++value);
            }
        });
    }

    public string Get()
    {
        #region удалено
        // Another another = _serviceProvider.GetRequiredService<Another>();
        #endregion удалено
        Session session = _serviceProvider.GetRequiredService<SessionHolder>().Session;
        #region добавлено
        Another another = session.RequestServiceProvider.GetRequiredService<Another>();
        #endregion добавлено
        _logger.LogInformation($"{this}({GetHashCode()}) {another}({another.GetHashCode()})");
        List<int> result = new();
        while (_queue.TryDequeue(out int k))
        {
            result.Add(k);
        }
        return $"{this}({GetHashCode()}) {another}({another.GetHashCode()}), {string.Join(", ", result)}";
    }

    public void Dispose()
    {
        _cancellationTokenSource.Cancel();
        _fill.Wait();
        _logger.LogInformation($"{this}({GetHashCode()}) disposed");
    }

}

public class Another : IDisposable
{
    private readonly ILogger<Another> _logger;

    public Another(ILogger<Another> logger) => _logger = logger;
    public void Dispose()
    {
        _logger.LogInformation($"{this}({GetHashCode()}) disposed");
    }
}



