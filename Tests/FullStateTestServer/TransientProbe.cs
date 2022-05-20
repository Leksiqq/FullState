namespace FullStateTestServer;

public class TransientProbe : BaseProbe, ITransient
{
    public TransientProbe(IServiceProvider services) : base(services)
    {
    }
}
