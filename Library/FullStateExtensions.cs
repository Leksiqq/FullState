using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace Net.Leksi.Server;

public static class FullStateExtensions
{

    private static readonly FullStateOptions _fullStateOptions = new();
    private static MemoryCacheEntryOptions _entryOptions = null!;
    private static System.Timers.Timer _checkSessions = null!;
    private static int _cookieSequenceGen = 0;

    public static IServiceCollection AddFullState(this IServiceCollection services, Action<FullStateOptions>? configure = null)
    {
        configure?.Invoke(_fullStateOptions);
        if (!services.Any(sd => sd.ServiceType == typeof(IMemoryCache)))
        {
            services.AddMemoryCache(op =>
            {
                op.ExpirationScanFrequency = _fullStateOptions.ExpirationScanFrequency;
            });
        }
        _entryOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(_fullStateOptions.IdleTimeout);
        PostEvictionCallbackRegistration postEvictionCallbackRegistration = new PostEvictionCallbackRegistration();
        postEvictionCallbackRegistration.State = typeof(FullStateExtensions);
        postEvictionCallbackRegistration.EvictionCallback = (k, v, r, s) =>
        {
            Console.WriteLine($"{k}, {v}, {r}, {s}");
            if(r is EvictionReason.Expired && s == typeof(FullStateExtensions) && v is IDisposable disposable)
            {
                disposable.Dispose();
            }
        };
        _entryOptions.PostEvictionCallbacks.Add(postEvictionCallbackRegistration);
        return services;
    }

    public static IApplicationBuilder UseFullState(this IApplicationBuilder app)
    {
        app.Use(async (context, next) =>
        {
            IMemoryCache sessions = context.RequestServices.GetRequiredService<IMemoryCache>();
            if(_checkSessions is null)
            {
                lock (app)
                {
                    if (_checkSessions is null)
                    {
                        _checkSessions = new(_fullStateOptions.ExpirationScanFrequency.TotalMilliseconds);
                        _checkSessions.Elapsed += (s, e) => 
                        {
                            sessions.TryGetValue(string.Empty, out object dumb);
                        };
                        _checkSessions.Enabled = true;
                        _checkSessions.AutoReset = true;
                    }
                }
            }
            object sessionObj = null;
            IServiceProvider sessionServiceProvider = null;
            int caseMatch = 0;
            Console.WriteLine($"{_fullStateOptions.Cookie.Name}");
            string key = context.Request.Cookies[_fullStateOptions.Cookie.Name];
            bool isNewSession = false;
            if(
                key is null && (caseMatch = 1) == caseMatch
                || !sessions.TryGetValue(key, out sessionObj) && (caseMatch = 2) == caseMatch
                || (sessionServiceProvider = sessionObj as IServiceProvider) is null && (caseMatch = 3) == caseMatch
            )
            {
                Console.WriteLine(caseMatch);
                key = $"{Guid.NewGuid()}:{Interlocked.Increment(ref _cookieSequenceGen)}";
                sessionServiceProvider = context.RequestServices.CreateScope().ServiceProvider;
                context.Response.Cookies.Append(_fullStateOptions.Cookie.Name, key, _fullStateOptions.Cookie.Build(context));
                isNewSession = true;
            }
            context.RequestServices = sessionServiceProvider;
            try
            {
                await next?.Invoke();
                if (isNewSession)
                {
                    sessions.Set(key, sessionServiceProvider, _entryOptions);
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        });
        return app;
    }

}
