namespace Net.Leksi.FullState;

internal class Session: IDisposable
{
    internal SemaphoreSlim OneRequestAllowed { get; init; } = new(1);

    internal IServiceProvider SessionServices { get; set; } = null!;

    internal CancellationTokenSource CancellationTokenSource { get; init; } = new();

    public void Dispose()
    {
        if (!CancellationTokenSource.IsCancellationRequested)
        {
            CancellationTokenSource.Cancel();
        }
        CancellationTokenSource.Dispose();
        if (SessionServices is IDisposable disposable)
        {
            disposable.Dispose();
        }
        OneRequestAllowed.Dispose();
    }

}
