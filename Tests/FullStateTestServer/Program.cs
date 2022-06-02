using FullStateTestServer;
using Net.Leksi.FullState;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddFullState(op =>
{
    op.IdleTimeout = TimeSpan.FromSeconds(20);
    op.Cookie.Name = "qq";
    if(args is { })
    {
        string logout = args.Where(s => s.StartsWith("logout=")).FirstOrDefault();
        if(logout is { })
        {
            op.LogoutPath = logout.Substring("logout=".Length);
        }
    }
});

builder.Services.AddScoped<IScoped, Probe>();
builder.Services.AddSingleton<ISingleton, Probe>();
builder.Services.AddTransient<ITransient, Probe>();

builder.Services.AddScoped<List<TraceItem>>();


WebApplication app = builder.Build();

app.UseFullState();

app.MapGet("/", async (HttpContext context) =>
{
    new Probe(context.RequestServices).DoSomething(string.Empty);

    context.RequestServices.GetRequiredService<List<TraceItem>>().ForEach(h => h.Session = context.Request.Cookies["qq"]);

    JsonSerializerOptions options = new();

    await context.Response.WriteAsJsonAsync(context.RequestServices.GetRequiredService<List<TraceItem>>(), options);
});

if (args is { })
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
        Probe.Depth = int.Parse(depth.Substring("depth=".Length));
    }
}

app.Run();

