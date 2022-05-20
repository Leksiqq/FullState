using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace Net.Leksi.FullState;
/// <summary>
/// <para xml:lang="ru">
/// Класс, предоставляющий расширения для <see cref="IServiceCollection"/> и <see cref="IApplicationBuilder"/> предназначенные для 
/// поддержки full state сервера
/// </para>
/// <para xml:lang="en">
/// A class that provides extensions to <see cref="IServiceCollection"/> and <see cref="IApplicationBuilder"/> for
/// full state server support
/// </para>
/// </summary>
public static class FullStateExtensions
{

    private static readonly FullStateOptions _fullStateOptions = new();
    private static MemoryCacheEntryOptions _entryOptions = null!;
    private static System.Timers.Timer _checkSessions = null!;
    private static int _cookieSequenceGen = 0;
    private static int _rootServiceProviderCheckState = 0;

    /// <summary>
    /// <para xml:lang="ru">
    /// Добавляет инфраструктуру, необходимую для full state сервера
    /// </para>
    /// <para xml:lang="en">
    /// Adds the infrastructure needed for a full state server
    /// </para>
    /// </summary>
    /// <param name="services">
    /// <para xml:lang="ru">
    /// Заданная коллекция
    /// </para>
    /// <para xml:lang="en">
    /// Given collection
    /// </para>
    /// </param>
    /// <param name="configure">
    /// <para xml:lang="ru">
    /// Предоставленные параметры для настройки
    /// </para>
    /// <para xml:lang="en">
    /// Provided options for customization
    /// </para>
    /// </param>
    /// <returns></returns>
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
        if (!services.Any(sd => sd.ServiceType == typeof(IHttpContextAccessor)))
        {
            services.AddHttpContextAccessor();
        }
        _entryOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(_fullStateOptions.IdleTimeout);
        PostEvictionCallbackRegistration postEvictionCallbackRegistration = new PostEvictionCallbackRegistration();
        postEvictionCallbackRegistration.State = typeof(FullStateExtensions);
        postEvictionCallbackRegistration.EvictionCallback = (k, v, r, s) =>
        {
            if (r is EvictionReason.Expired && s is Type stype && stype == typeof(FullStateExtensions) 
                && v is SessionHolder session && session.ServiceProvider is IDisposable disposable)
            {
                disposable.Dispose();
            }
        };
        _entryOptions.PostEvictionCallbacks.Add(postEvictionCallbackRegistration);

        services.AddScoped<IFullState, FullState>();
        services.AddSingleton<SingletonProbe>();


        return services;
    }
    /// <summary>
    /// <para xml:lang="ru">
    /// Добавляет ПО промежуточного слоя для автоматического включения поддержки full state сервера
    /// </para>
    /// /// <para xml:lang="en">
    /// Adds middleware to automatically enable server full state support
    /// </para>
    /// </summary>
    /// <param name="app">
    /// <see cref="IApplicationBuilder"/>
    /// </param>
    /// <returns></returns>
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
            SessionHolder? session = null!;
            string key = context.Request.Cookies[_fullStateOptions.Cookie.Name];
            bool isNewSession = false;
            FullState sessionFullState = null!;
            FullState contextFullState = (context.RequestServices.GetRequiredService<IFullState>() as FullState)!;
            FullState singletonFullState = null!;
            if (_rootServiceProviderCheckState != 2)
            {

            }
            try
            {
                singletonFullState = context.RequestServices.GetRequiredService<SingletonProbe>().ServiceProvider.GetRequiredService<IFullState>() as FullState;
                _rootServiceProviderCheckState = 1;
            }
            catch (InvalidOperationException)
            {
                _rootServiceProviderCheckState = 2;
            }
            if (
                key is null
                || !sessions.TryGetValue(key, out sessionObj)
                || (session = sessionObj as SessionHolder) is null
            )
            {
                key = $"{Guid.NewGuid()}:{Interlocked.Increment(ref _cookieSequenceGen)}";
                session = new SessionHolder(context.RequestServices.CreateScope().ServiceProvider);
                sessionFullState = (session!.ServiceProvider!.GetRequiredService<IFullState>() as FullState)!;
                sessionFullState.SessionServices = session!.ServiceProvider!;
                if (singletonFullState is { })
                {
                    singletonFullState.SessionServices = session!.ServiceProvider!;
                }
                context.Response.Cookies.Append(_fullStateOptions.Cookie.Name, key, _fullStateOptions.Cookie.Build(context));
                isNewSession = true;
            }
            else
            {
                sessionFullState = (session!.ServiceProvider!.GetRequiredService<IFullState>() as FullState)!;
            }
            sessionFullState.RequestServices = context.RequestServices;
            contextFullState.RequestServices = context.RequestServices;
            contextFullState.SessionServices = session!.ServiceProvider!;
            if (singletonFullState is { })
            {
                singletonFullState.RequestServices = context.RequestServices;
            }
            try
            {
                await (next?.Invoke() ?? Task.CompletedTask);
                if (isNewSession)
                {
                    sessions.Set(key, session, _entryOptions);
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
