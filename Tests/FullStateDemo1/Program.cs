using Net.Leksi.Server;
using WebApplication1;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddFullState(op =>
{
    op.IdleTimeout = TimeSpan.FromSeconds(20);
});
builder.Services.AddScoped<InfoProvider>();

var app = builder.Build();

app.UseFullState();

app.MapGet("/", /*[Authorize] */async (HttpContext httpContext) =>
{
    await httpContext.Response.WriteAsJsonAsync<List<int>>(httpContext.RequestServices.GetRequiredService<InfoProvider>().Get());
});

app.Run();
