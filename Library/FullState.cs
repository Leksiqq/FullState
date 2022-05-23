namespace Net.Leksi.FullState;

internal class FullState : IFullState
{
    public IServiceProvider RequestServices { get; internal set; } = null!;

    public IServiceProvider SessionServices { get; internal set; } = null!;
}
