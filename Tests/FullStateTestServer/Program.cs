using FullStateTestServer;
using Net.Leksi.FullState;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddFullState(op =>
{
    op.IdleTimeout = TimeSpan.FromSeconds(20);
    op.Cookie.Name = "qq";
});

builder.Services.AddScoped<IScoped, ScopedProbe>();
builder.Services.AddSingleton<ISingleton, SingletonProbe>();
builder.Services.AddTransient<ITransient, TransientProbe>();

builder.Services.AddScoped<List<AssertHolder>>();


WebApplication app = builder.Build();

app.UseFullState();

app.MapGet("/", async (HttpContext context) =>
{
    new BaseProbe(context.RequestServices).DoSomething(string.Empty);

    context.RequestServices.GetRequiredService<List<AssertHolder>>().ForEach(h => h.Session = context.Request.Cookies["qq"]);

    JsonSerializerOptions options = new();

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

