using Net.Leksi.FullState;
using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddFullState(op =>
{
    op.Cookie.Name = "qq";
    op.ExpirationScanFrequency = TimeSpan.FromSeconds(1);
    op.IdleTimeout = TimeSpan.FromSeconds(20);
    op.LogoutPath = "/logout";
});

builder.Services.AddScoped<InfoProvider>();
builder.Services.AddScoped<InfoProvider>();

builder.Services.AddScoped<Another>();

builder.Logging.ClearProviders();
builder.Logging.AddConsole(op =>
{
    op.TimestampFormat = "[HH:mm:ss:fff] ";
});

var app = builder.Build();

app.UseFullState();

app.MapGet("/api/{cancel=false}", async (HttpContext context, bool cancel) =>
{
    Another another = context.RequestServices.GetRequiredService<Another>();
    await context.Response.WriteAsync($"[{DateTime.Now.ToString("HH:mm:ss.fff")}] Hello, World! \n"
        + $", controller {another}({another.GetHashCode()}), \n"
        + context.RequestServices.GetFullState().SessionServices.GetServices<InfoProvider>().First().Get(false) + "\n"
        + context.RequestServices.GetFullState().SessionServices.GetServices<InfoProvider>().Last().Get(cancel) + "\n"
        );
});

app.Run();

public class InfoProvider : IDisposable
{
    private ConcurrentQueue<int> _queue = new();
    private Task _fill = null!;
    private readonly ILogger<InfoProvider> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly CancellationTokenSource _cancellationTokenSource;

    public InfoProvider(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _logger = _serviceProvider.GetRequiredService<ILogger<InfoProvider>>();
        _cancellationTokenSource = _serviceProvider.GetFullState().CreateCancellationTokenSource();
        int value = 0;
        CancellationToken cancellationToken = _cancellationTokenSource.Token;
        _fill = Task.Run(async () =>
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(1000);
                _queue.Enqueue(++value);
            }
            _logger.LogInformation($"({GetHashCode()}) CancellationRequested");
        });
    }

    public string Get(bool cancel)
    {
        Another another = _serviceProvider.GetFullState().RequestServices.GetRequiredService<Another>();
        _logger.LogInformation($"{this}({GetHashCode()}) {another}({another.GetHashCode()})");
        List<int> result = new();
        while (_queue.TryDequeue(out int k))
        {
            result.Add(k);
        }
        if (cancel)
        {
            _logger.LogInformation($"{this}({GetHashCode()}) Cancel by request");
            _cancellationTokenSource.Cancel();
        }
        return $"{this}({GetHashCode()}) {another}({another.GetHashCode()}), {string.Join(", ", result)}";
    }

    public void Dispose()
    {
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



