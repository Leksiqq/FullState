using Microsoft.Extensions.DependencyInjection;

namespace Net.Leksi.Server;

public class SessionalServiceProvider : IServiceProvider, IDisposable
{
    internal static Dictionary<Type, int> SessionalServices { get; private set; } = new();

    internal IServiceProvider SourceServiceProvider { get; set; }
    internal IServiceProvider ScopedServiceProvider { get ; set; }

    public object? GetService(Type serviceType)
    {
        if (SessionalServices.ContainsKey(serviceType))
        {
            return ScopedServiceProvider.GetService(serviceType);
        }
        return SourceServiceProvider.GetService(serviceType);
    }

    public void Dispose()
    {
        if(ScopedServiceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
