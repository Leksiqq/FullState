using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace Net.Leksi.FullState;

public static class FullStateExtensions
{

    private static readonly FullStateOptions _fullStateOptions = new();
    private static MemoryCacheEntryOptions _entryOptions = null!;
    private static System.Timers.Timer _checkSessions = null!;
    private static int _cookieSequenceGen = 0;

    public static IServiceCollection AddSessional(this IServiceCollection services, Type implementationType)
    {
        services.AddScoped(implementationType);
        FullState.SessionalServices.Add(implementationType);
        return services;
    }

    public static IServiceCollection AddSessional(this IServiceCollection services, Type serviceType, Func<IServiceProvider, object> implementationFactory)
    {
        services.AddScoped(serviceType, implementationFactory);
        FullState.SessionalServices.Add(serviceType);
        return services;
    }

    public static IServiceCollection AddSessional(this IServiceCollection services, Type serviceType, Type implementationType)
    {
        services.AddScoped(serviceType, implementationType);
        FullState.SessionalServices.Add(serviceType);
        return services;
    }

    public static IServiceCollection AddSessional<TService, TImplementation>(this IServiceCollection services)
        where TService : class
        where TImplementation : class, TService
    {
        services.AddScoped<TService, TImplementation>();
        FullState.SessionalServices.Add(typeof(TService));
        return services;
    }

    public static IServiceCollection AddSessional<TService, TImplementation>(this IServiceCollection services, Func<IServiceProvider, TImplementation> implementationFactory)
        where TService : class
        where TImplementation : class, TService
    {
        services.AddScoped<TService, TImplementation>(implementationFactory);
        FullState.SessionalServices.Add(typeof(TService));
        return services;
    }

    public static IServiceCollection AddSessional<TService>(this IServiceCollection services)
        where TService : class
    {
        services.AddScoped<TService>();
        FullState.SessionalServices.Add(typeof(TService));
        return services;
    }

    public static IServiceCollection AddSessional<TService>(this IServiceCollection services, Func<IServiceProvider, TService> implementationFactory)
        where TService : class
    {
        services.AddScoped<TService>(implementationFactory);
        FullState.SessionalServices.Add(typeof(TService));
        return services;
    }

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
            if(r is EvictionReason.Expired && s is Type stype && stype == typeof(FullStateExtensions) && v is FullState session
                && session.SessionServiceProvider is IDisposable disposable)
            {
                disposable.Dispose();
            }
        };
        _entryOptions.PostEvictionCallbacks.Add(postEvictionCallbackRegistration);

        services.AddScoped<FullStateHolder>();
        services.AddSessional<IFullState>(op => op.GetRequiredService<FullStateHolder>().FullState);


        return services;
    }

    public static IApplicationBuilder UseFullState(this IApplicationBuilder app)
    {
        app.Use(async (context, next) =>
        {
            IMemoryCache sessions = context.RequestServices.GetRequiredService<IMemoryCache>();
            if (_checkSessions is null)
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
            object? sessionObj = null;
            FullState? fullState = null;
            string key = context.Request.Cookies[_fullStateOptions.Cookie.Name];
            bool isNewSession = false;
            if (
                key is null
                || !sessions.TryGetValue(key, out sessionObj)
                || (fullState = sessionObj as FullState) is null
            )
            {
                key = $"{Guid.NewGuid()}:{Interlocked.Increment(ref _cookieSequenceGen)}";
                fullState = new FullState(context.RequestServices.CreateScope().ServiceProvider);
                fullState.SessionServiceProvider.GetRequiredService<FullStateHolder>().FullState = fullState;
                context.Response.Cookies.Append(_fullStateOptions.Cookie.Name, key, _fullStateOptions.Cookie.Build(context));
                isNewSession = true;
            }
            fullState!.RequestServices = context.RequestServices;
            context.RequestServices = fullState;
            try
            {
                await (next?.Invoke() ?? Task.CompletedTask);
                if (isNewSession)
                {
                    sessions.Set(key, fullState, _entryOptions);
                }
            }
            catch (Exception)
            {
                throw;
            }
        });
        return app;
    }


}
