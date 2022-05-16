namespace Net.Leksi.Server;

internal class FullState : IFullState, IServiceProvider
{
    internal static HashSet<Type> SessionalServices { get; private set; } = new();

    internal IServiceProvider SessionServiceProvider { get; set; }
    public IServiceProvider RequestServices { get; set; } = null!;

    public FullState(IServiceProvider serviceProvider) => SessionServiceProvider = serviceProvider;

    public object? GetService(Type serviceType)
    {
        if (SessionalServices.Contains(serviceType))
        {
            return SessionServiceProvider.GetService(serviceType);
        }
        return RequestServices.GetService(serviceType);
    }

}
