using FullStateTestServer;
using Net.Leksi.FullState;


var builder = WebApplication.CreateBuilder(new string[] { });

builder.Services.AddFullState(op =>
{
    op.IdleTimeout = TimeSpan.FromSeconds(20);
    op.Cookie.Name = "qq";
});

builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<ClientHolder>();

builder.Services.AddScoped<ScopedProbe>();
builder.Services.AddSingleton<SingletonProbe>();
builder.Services.AddTransient<TransientProbe>();

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

    await context.Response.WriteAsJsonAsync(context.RequestServices.GetRequiredService<List<AssertHolder>>());
});

app.Run();

