namespace FullStateTestServer;

public class ScopedProbe : BaseProbe, IScoped
{
    public ScopedProbe(IServiceProvider services) : base(services)
    {
    }
}
