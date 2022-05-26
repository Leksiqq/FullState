namespace Net.Leksi.FullState;

internal class Session: IDisposable
{
    internal SemaphoreSlim OneRequestAllowed { get; init; } = new(1);

    public IServiceProvider SessionServices { get; internal set; } = null!;

    public void Dispose()
    {
        if(SessionServices is IDisposable disposable)
        {
            disposable.Dispose();
        }
        OneRequestAllowed.Dispose();
    }
}
