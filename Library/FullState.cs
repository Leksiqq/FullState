namespace Net.Leksi.FullState;

internal class FullState : IFullState
{
    public IServiceProvider RequestServices { get; internal set; }

    public IServiceProvider SessionServices { get; internal set; }
}
