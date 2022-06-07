using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Net.Leksi.FullState;

public class MemoryCacheCleaner : BackgroundService
{
    private readonly IMemoryCache _cache;
    private readonly TimeSpan _frequency;

    public MemoryCacheCleaner(IServiceProvider serviceProvider) =>
        (_cache, _frequency) = (
            serviceProvider.GetRequiredService<IMemoryCache>(), 
            serviceProvider.GetRequiredService<IOptionsMonitor<FullStateOptions>>().CurrentValue.ExpirationScanFrequency
        );

    protected override async Task ExecuteAsync(CancellationToken stopToken)
    {
        for (; !stopToken.IsCancellationRequested; )
        {
            await Task.Delay(_frequency, stopToken);
            _cache.TryGetValue(string.Empty, out var _);
        }
    }

}
