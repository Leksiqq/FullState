using Microsoft.Extensions.Caching.Memory;
using Net.Leksi.Server;
using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddFullState(op =>
{
    op.Cookie.Name = "qq";
    op.ExpirationScanFrequency = TimeSpan.FromSeconds(1);
    op.IdleTimeout = TimeSpan.FromSeconds(20);
});

builder.Services.AddSessional<InfoProvider>();

builder.Services.AddScoped<Another>();

builder.Logging.ClearProviders();
builder.Logging.AddConsole(op =>
{
    op.TimestampFormat = "[HH:mm:ss:fff] ";
});

var app = builder.Build();

app.UseFullState();

app.MapGet("/", async context =>
{
    Another another = context.RequestServices.GetRequiredService<Another>();
    await context.Response.WriteAsync($"[{DateTime.Now.ToString("HH:mm:ss.fff")}] Hello, World! " 
        + $", controller {another}({another.GetHashCode()}), "
        + context.RequestServices.GetRequiredService<InfoProvider>().Get());
});

app.Run();

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
        IFullState session = _serviceProvider.GetRequiredService<IFullState>();
        Another another = session.RequestServices.GetRequiredService<Another>();
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



