using Microsoft.AspNetCore.Diagnostics;
using Net.Leksi.FullState;

namespace FullStateTestProject;

public class Server
{

    private WebApplication app;
    public ManualResetEventSlim IsRunning { get; init; } = new();
    public Uri Uri { get; private set; } 

    public StatHolder StatHolder { get; init; } = new();

    public async Task StopAsync()
    {
        await app.StopAsync();
    }

    public void Run()
    {
        IsRunning.Reset();

        var builder = WebApplication.CreateBuilder(new string[] { });

        builder.Services.AddFullState(op =>
        {
            op.IdleTimeout = TimeSpan.FromSeconds(5);
            op.Cookie.Name = "qq";
        });

        builder.Services.AddSessional<InfoProvider>();

        builder.Services.AddScoped<Another>();

        builder.Services.AddSingleton<StatHolder>(op => StatHolder);

        builder.Services.AddScoped<ClientHolder>();

        app = builder.Build();

        app.UseFullState();

        app.MapGet("/{client}/{request}", async (HttpContext context, int client, int request) =>
        {
            IFullState session = context.RequestServices.GetRequiredService<IFullState>();

            StatHolder statHolder = context.RequestServices.GetRequiredService<StatHolder>();
            ClientHolder clientHolder = session.RequestServices.GetRequiredService<ClientHolder>();
            clientHolder.Client = client;
            clientHolder.Request = request;
            clientHolder.Session = context.Request.Cookies["qq"];

            statHolder.Asserts.Enqueue(new AssertHolder
            {
                Client = clientHolder.Client,
                Request = clientHolder.Request,
                Session = clientHolder.Session,
                Selector = "session",
                Value = session.GetHashCode()
            });

            Another another1 = context.RequestServices.GetRequiredService<Another>();

            statHolder.Asserts.Enqueue(new AssertHolder
            {
                Client = clientHolder.Client,
                Request = clientHolder.Request,
                Session = clientHolder.Session,
                Selector = "another1",
                Value = another1.GetHashCode()
            });

            another1.DoSomething();

            Another another2 = session.RequestServices.GetRequiredService<Another>();

            statHolder.Asserts.Enqueue(new AssertHolder
            {
                Client = clientHolder.Client,
                Request = clientHolder.Request,
                Session = clientHolder.Session,
                Selector = "another2",
                Value = another2.GetHashCode()
            });

            another2.DoSomething();

            InfoProvider infoProvider1 = context.RequestServices.GetRequiredService<InfoProvider>();

            statHolder.Asserts.Enqueue(new AssertHolder
            {
                Client = clientHolder.Client,
                Request = clientHolder.Request,
                Session = clientHolder.Session,
                Selector = "infoProvider1",
                Value = infoProvider1.GetHashCode()
            });

            infoProvider1.DoSomething();

            InfoProvider infoProvider2 = session.RequestServices.GetRequiredService<InfoProvider>();

            statHolder.Asserts.Enqueue(new AssertHolder
            {
                Client = clientHolder.Client,
                Request = clientHolder.Request,
                Session = clientHolder.Session,
                Selector = "infoProvider2",
                Value = infoProvider2.GetHashCode()
            });

            infoProvider2.DoSomething();

            await context.Response.WriteAsync($"#{request} Hello, Client #{client}!");
        });

        app.Lifetime.ApplicationStarted.Register(() => 
        {
            Uri = new Uri(app.Urls.First());
            IsRunning.Set(); 
        });

        app.Run();


    }
}
