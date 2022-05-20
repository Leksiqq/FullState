using FullStateTestServer;
using Net.Leksi.FullState;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddFullState(op =>
{
    op.IdleTimeout = TimeSpan.FromSeconds(20);
    op.Cookie.Name = "qq";
});

builder.Services.AddScoped<ClientHolder>();

builder.Services.AddScoped<IScoped, ScopedProbe>();
builder.Services.AddSingleton<ISingleton, SingletonProbe>();
builder.Services.AddTransient<ITransient, TransientProbe>();

builder.Services.AddScoped<List<AssertHolder>>();


WebApplication app = builder.Build();

app.UseFullState();

app.MapGet("/{client}/{request}", async (HttpContext context, int client, int request) =>
{
    Console.WriteLine(context.Request.Path);
    ClientHolder clientHolder = context.RequestServices.GetRequiredService<ClientHolder>();
    clientHolder.Client = client;
    clientHolder.Request = request;
    clientHolder.Session = context.Request.Cookies["qq"];


    new BaseProbe(context.RequestServices).DoSomething(string.Empty);

    JsonSerializerOptions options = new();

    //Console.WriteLine(string.Join("\n", context.RequestServices.GetRequiredService<List<AssertHolder>>()));

    await context.Response.WriteAsJsonAsync(context.RequestServices.GetRequiredService<List<AssertHolder>>(), options);
});

if(args is { })
{
    string url = args.Where(s => s.StartsWith("applicationUrl=")).FirstOrDefault();
    if(url is { })
    {
        app.Urls.Clear();
        app.Urls.Add(url.Substring("applicationUrl=".Length));
    }
    string depth = args.Where(s => s.StartsWith("depth=")).FirstOrDefault();
    if(depth is { })
    {
        BaseProbe.Depth = int.Parse(depth.Substring("depth=".Length));
    }
}

app.Run();

