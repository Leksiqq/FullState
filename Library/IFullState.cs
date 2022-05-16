namespace Net.Leksi.Server;

public interface IFullState: IServiceProvider
{
    IServiceProvider RequestServices { get; }
}
