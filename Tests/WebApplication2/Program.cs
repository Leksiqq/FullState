using Microsoft.Extensions.Options;

string cookieName = "qq";
int cookieSequenceGen = 0;

var builder = WebApplication.CreateBuilder(new string[] { });

builder.Services.AddScoped<FullState1>();

WebApplication app = builder.Build();

app.Use(async (HttpContext context, Func<Task> next) =>
{
    string? key = context.Request.Cookies[cookieName];
    if(key is null)
    {
        key = $"{Guid.NewGuid()}:{Interlocked.Increment(ref cookieSequenceGen)}";
        context.Response.Cookies.Append(cookieName, key, new CookieBuilder().Build(context));
    }
    context.RequestServices.GetRequiredService<FullState1>().Session = context.RequestServices.GetRequiredService<IOptionsMonitor<Session1>>().Get(key);
    ++context.RequestServices.GetRequiredService<FullState1>().Session.RequestsCounter;

    next?.Invoke();
});

app.MapGet("/api", async (HttpContext context) =>
{
    Session1 session = context.RequestServices.GetRequiredService<FullState1>().Session;
    await context.Response.WriteAsync($"Hello, Client {session.GetHashCode()} (#{session.RequestsCounter})!");
});

app.Run();

public class FullState1
{
    internal Session1 Session { get; set; }
}

internal class Session1
{
    public int RequestsCounter { get; set; } = 0;
}