using System.Collections.Concurrent;

namespace FullStateDemo;

public class InfoProvider: IDisposable
{
    private ConcurrentQueue<int> _queue = new();
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private Task _fill = null;
    private readonly ILogger<InfoProvider> _logger;

    public InfoProvider(ILogger<InfoProvider> logger)
    {
        _logger = logger;
        int value = 0;
        CancellationToken cancellationToken = _cancellationTokenSource.Token;
        _fill = Task.Run(async () =>
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(200);
                //Console.WriteLine($"{GetHashCode()}: {value}");
                _queue.Enqueue(++value);
            }
        });
    }

    public List<int> Get()
    {
        List<int> result = new();
        while (_queue.TryDequeue(out int k))
        {
            result.Add(k);
        }
        return result;
    }

    public void Dispose()
    {
        _cancellationTokenSource.Cancel();
        _fill.Wait();
        _logger.LogInformation($"{this}({GetHashCode()}) disposed");
    }
}
