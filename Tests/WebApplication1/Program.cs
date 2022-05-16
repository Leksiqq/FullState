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
    if (r is EvictionReason.Expired && s is Type sType && sType == typeof(Program) && v is Session session 
        && session.SessionServiceProvider is IDisposable disposable)
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
builder.Services.AddScoped<Session>(op => op.GetRequiredService<SessionHolder>().Session);
Session.SessionalServices.Add(typeof(Session));

builder.Services.AddScoped<InfoProvider>();
Session.SessionalServices.Add(typeof(InfoProvider));

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
        session.SessionServiceProvider.GetRequiredService<SessionHolder>().Session = session;
        context.Response.Cookies.Append(cookieName, key);
        isNewSession = true;
    }

    ILogger<Program> logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    logger.LogInformation($"{context.Connection.Id}: {context.Request.Path}: {session}({session.GetHashCode()})");

    session.RequestServiceProvider = context.RequestServices;
    context.RequestServices = session;


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
    Another another = context.RequestServices.GetRequiredService<Another>();
    await context.Response.WriteAsync($"[{DateTime.Now.ToString("HH:mm:ss.fff")}] Hello, World! " 
        + $", controller {another}({another.GetHashCode()}), "
        + context.RequestServices.GetRequiredService<InfoProvider>().Get());
});

app.Run();

public class Session : IDisposable, IServiceProvider
{
    private readonly ILogger<Session> _logger;
    internal static HashSet<Type> SessionalServices { get; set; } = new();
    public IServiceProvider SessionServiceProvider { get; init; }
    public IServiceProvider RequestServiceProvider { get; set; }

    public Session(IServiceProvider serviceProvider) => 
        (SessionServiceProvider, _logger) = (serviceProvider, serviceProvider.GetRequiredService<ILogger<Session>>());
    public void Dispose()
    {
        _logger.LogInformation($"{this}({GetHashCode()}) disposed");
    }

    public object? GetService(Type serviceType)
    {
        if (SessionalServices.Contains(serviceType))
        {
            return SessionServiceProvider.GetService(serviceType);
        }
        return RequestServiceProvider.GetService(serviceType);
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
        Session session = _serviceProvider.GetRequiredService<Session>();
        Another another = session.RequestServiceProvider.GetRequiredService<Another>();
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



