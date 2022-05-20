namespace FullStateTestServer;

public class SingletonProbe : BaseProbe, ISingleton
{
    public SingletonProbe(IServiceProvider services) : base(services)
    {
    }
}
