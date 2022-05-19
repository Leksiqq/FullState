namespace Net.Leksi.FullState;

internal class SessionHolder
{
    internal IServiceProvider ServiceProvider { get; init; }

    internal SessionHolder(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
    }
}
