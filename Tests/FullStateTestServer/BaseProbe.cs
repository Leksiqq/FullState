using Net.Leksi.FullState;
using System.Diagnostics;

namespace FullStateTestServer;

public class BaseProbe : IDisposable
{
    private readonly IServiceProvider _services;
    private readonly List<AssertHolder> _asserts;
    private readonly ClientHolder _client;
    private readonly Type[] _types = new[] { typeof(TransientProbe), typeof(ScopedProbe), typeof(SingletonProbe) };

    public BaseProbe(IServiceProvider services)
    {
        Trace.WriteLine($"{GetType()}, {services.GetHashCode()}");
        _services = services;
        IFullState session = IFullState.Extract(_services);
        _client = session.RequestServices.GetRequiredService<ClientHolder>();
        _asserts = session.RequestServices.GetRequiredService<List<AssertHolder>>();
    }

    private void AddAssert(string selector, int value)
    {
        _asserts.Add(new AssertHolder
        {
            Client = _client.Client,
            Request = _client.Request,
            Session = _client.Session,
            Selector = selector,
            Value = value
        });
    }

    public void DoSomething(string trace)
    {
        AddAssert(trace, GetHashCode());
        if (trace.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries).Length < 4)
        {
            IFullState session = IFullState.Extract(_services);

            IServiceProvider[] services = new[] { _services, session.RequestServices, session.SessionServices };

            foreach (Type type in _types)
            {
                for (int i = 0; i < services.Length; i++)
                {
                    string nextTrace = $"{trace}/{type.Name}{i}";
                    try
                    {
                        BaseProbe probe = (BaseProbe)services[i].GetRequiredService(type);
                        probe.DoSomething(nextTrace);
                    }
                    catch
                    {
                        AddAssert(nextTrace, 0);
                    }
                }
            }
        }

    }

    public void Dispose()
    {
        AddAssert($"disposed", GetHashCode());
    }
}
