namespace Net.Leksi.FullState;

public class FullStateAccessor : IFullStateAccessor
{
    public IFullState Instance { get; internal set; }

    public FullStateAccessor()
    {
        Console.WriteLine("new FullStateAccessor()");
    }
}
