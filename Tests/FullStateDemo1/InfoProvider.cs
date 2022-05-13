using System.Collections.Concurrent;

namespace WebApplication1;

public class InfoProvider: IDisposable
{
    private ConcurrentQueue<int> _queue = new();
    private bool _running = true;
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private Task _fill = null;

    public InfoProvider()
    {
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
        Console.WriteLine($"{this} disposed");
        throw new Exception("ooooooooops!");
    }
}
