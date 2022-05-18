using Net.Leksi.FullState;

public class Another : IDisposable
{
    private readonly IServiceProvider _services;
    public bool IsDisposed { get; private set; } = false;
    public Another(IServiceProvider services) => _services = services;

    public void Dispose()
    {
        IsDisposed = true;
    }

    public void DoSomething()
    {
        IFullState session = _services.GetRequiredService<IFullState>();

        StatHolder statHolder = _services.GetRequiredService<StatHolder>();
        ClientHolder clientHolder = session.RequestServices.GetRequiredService<ClientHolder>();

        statHolder.Asserts.Enqueue(new AssertHolder
        {
            Client = clientHolder.Client,
            Request = clientHolder.Request,
            Session = clientHolder.Session,
            Selector = "session",
            Value = session.GetHashCode()
        });

        InfoProvider infoProvider3 = _services.GetRequiredService<InfoProvider>();

        statHolder.Asserts.Enqueue(new AssertHolder
        {
            Client = clientHolder.Client,
            Request = clientHolder.Request,
            Session = clientHolder.Session,
            Selector = "infoProvider3",
            Value = infoProvider3.GetHashCode()
        });

        InfoProvider infoProvider4 = session.RequestServices.GetRequiredService<InfoProvider>();

        statHolder.Asserts.Enqueue(new AssertHolder
        {
            Client = clientHolder.Client,
            Request = clientHolder.Request,
            Session = clientHolder.Session,
            Selector = "infoProvider4",
            Value = infoProvider4.GetHashCode()
        });
    }
}
