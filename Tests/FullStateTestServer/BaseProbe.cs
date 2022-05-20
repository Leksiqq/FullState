using Net.Leksi.FullState;

namespace FullStateTestServer;

public class BaseProbe : IDisposable
{
    private static int _genId = 0;
    private readonly IServiceProvider _services;
    private readonly Type[] _types = new[] { typeof(ITransient), typeof(IScoped), typeof(ISingleton) };

    internal static int Depth { get; set; } = 4;
    public int Id { get; private set; }
    public bool IsDisposed { get; private set; } = false;
    public BaseProbe(IServiceProvider services)
    {
        Id = Interlocked.Increment(ref _genId);
        _services = services;
    }

    private void AddAssert(string selector, int value, string? error = null)
    {
        IFullState session = _services.GetFullState();
        ClientHolder client = session.RequestServices.GetRequiredService<ClientHolder>();
        session.RequestServices.GetRequiredService<List<AssertHolder>>().Add(new AssertHolder
        {
            Client = client.Client,
            Request = client.Request,
            Session = client.Session,
            Selector = selector,
            ObjectId = value,
            Error = error
        });
    }

    public void DoSomething(string trace)
    {
        if (!string.IsNullOrEmpty(trace))
        {
            AddAssert(trace, Id, IsDisposed ? "disposed" : null);
        }
        if (trace.Where(c => c == '/').Count() < Depth)
        {
            IFullState session = _services.GetFullState();

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
                    catch (Exception ex)
                    {
                        AddAssert(nextTrace, -1, ex.ToString());
                    }
                }
            }
        }

    }

    public void Dispose()
    {
        IsDisposed = true;
    }
}
