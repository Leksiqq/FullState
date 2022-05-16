using FullStateDemo;
using Net.Leksi.FullState;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole(op =>
{
    op.TimestampFormat = "[HH:mm:ss.fff] ";
});

builder.Services.AddFullState(op =>
{
    op.IdleTimeout = TimeSpan.FromSeconds(20);
});
builder.Services.AddSessional<InfoProvider>();
builder.Services.AddScoped<Another>();

var app = builder.Build();

app.UseFullState();

app.MapGet("/", async (HttpContext httpContext) =>
{
    ILogger<Program> logger = httpContext.RequestServices.GetRequiredService<ILogger<Program>>();
    InfoProvider infoProvider = httpContext.RequestServices.GetRequiredService<InfoProvider>();
    Another another = httpContext.RequestServices.GetRequiredService<Another>();

    logger.LogInformation($"{httpContext.Connection.Id}: {another}({another.GetHashCode()}), {infoProvider}({infoProvider.GetHashCode()})");
    await httpContext.Response.WriteAsJsonAsync<List<int>>(infoProvider.Get());
});

app.Run();
