namespace Net.Leksi.FullState;

internal class SingletonProbe
{
    internal IServiceProvider ServiceProvider { get; init; }
    public SingletonProbe(IServiceProvider serviceProvider) => ServiceProvider = serviceProvider;
}
