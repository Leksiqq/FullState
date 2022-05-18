namespace Net.Leksi.FullState;

internal class FullState : IFullState, IDisposable
{
    public IServiceProvider RequestServices { get; internal set; } = null!;

    public IServiceProvider SessionServices { get; internal set; } = null!;

    public void Dispose()
    {
        Console.WriteLine($"Dispose({GetHashCode()})");
        if(SessionServices is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
