namespace Net.Leksi.FullState;

internal class FullState : IFullState
{
    internal Session Session { get; set; } = null!;

    public IServiceProvider RequestServices { get; internal set; } = null!;

    public IServiceProvider SessionServices => Session.SessionServices;

    public CancellationTokenSource CreateCancellationTokenSource()
    {
        return CancellationTokenSource.CreateLinkedTokenSource(Session.CancellationTokenSource.Token);
    }
    
}
