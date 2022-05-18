using Net.Leksi.FullState;

public class InfoProvider : IDisposable
{
    private readonly IServiceProvider _services;
    public bool IsDisposed { get; private set; } = false;
    public InfoProvider(IServiceProvider services) => _services = services;

    public void Dispose()
    {
        IsDisposed = true;
    }

    public void DoSomething()
    {
        IFullState session = _services.GetRequiredService<IFullState>();

        StatHolder statHolder = _services.GetRequiredService<StatHolder>();
        string client = session.RequestServices.GetRequiredService<ClientHolder>().Client;

        statHolder.Asserts.Enqueue(new AssertHolder
        {
            Client = statHolder.CommonValues[client].Client,
            Session = statHolder.CommonValues[client].Session,
            Selector = "session",
            Value = session.GetHashCode()
        });

        Another another3 = _services.GetRequiredService<Another>();

        statHolder.Asserts.Enqueue(new AssertHolder
        {
            Client = statHolder.CommonValues[client].Client,
            Session = statHolder.CommonValues[client].Session,
            Selector = "another3",
            Value = another3.GetHashCode()
        });

        Another another4 = session.RequestServices.GetRequiredService<Another>();

        statHolder.Asserts.Enqueue(new AssertHolder
        {
            Client = statHolder.CommonValues[client].Client,
            Session = statHolder.CommonValues[client].Session,
            Selector = "another4",
            Value = another4.GetHashCode()
        });

    }
}
