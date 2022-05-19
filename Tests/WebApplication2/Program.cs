using FullStateTestServer;
using Net.Leksi.FullState;
using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(new string[] { });

builder.Services.AddFullState(op =>
{
    op.IdleTimeout = TimeSpan.FromSeconds(5);
    op.Cookie.Name = "qq";
});


builder.Services.AddSingleton<ConcurrentQueue<AssertHolder>>();
builder.Services.AddScoped<ClientHolder>();

builder.Services.AddScoped<ScopedProbe>();
builder.Services.AddSingleton<SingletonProbe>();
builder.Services.AddTransient<TransientProbe>();
builder.Services.AddTransient<BaseProbe>();


WebApplication app = builder.Build();

app.UseFullState();

app.MapGet("/{client}/{request}", async (HttpContext context, int client, int request) =>
{

    ClientHolder clientHolder = context.RequestServices.GetRequiredService<ClientHolder>();
    clientHolder.Client = client;
    clientHolder.Request = request;
    clientHolder.Session = context.Request.Cookies["qq"];


    context.RequestServices.GetRequiredService<BaseProbe>().DoSomething(string.Empty);

    await context.Response.WriteAsync($"#{request} Hello, Client #{client}!");
});

app.Run();
