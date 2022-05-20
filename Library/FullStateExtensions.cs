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
    ///  (Расширение)
    /// Добавляет инфраструктуру, необходимую для full state сервера
    /// </para>
    /// <para xml:lang="en">
    /// (Extension)
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
                && v is IDisposable disposable)
            {
                disposable.Dispose();
            }
        };
        _entryOptions.PostEvictionCallbacks.Add(postEvictionCallbackRegistration);

        services.AddScoped<IFullState, FullState>();


        return services;
    }
    /// <summary>
    /// <para xml:lang="ru">
    /// (Расширение)
    /// Добавляет ПО промежуточного слоя для автоматического включения поддержки full state сервера
    /// </para>
    /// <para xml:lang="en">
    /// (Extension)
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
            IServiceProvider? session = null!;
            string key = context.Request.Cookies[_fullStateOptions.Cookie.Name];
            bool isNewSession = false;
            FullState fullState = (context.RequestServices.GetRequiredService<IFullState>() as FullState)!;
            if (
                key is null
                || !sessions.TryGetValue(key, out sessionObj)
                || (session = sessionObj as IServiceProvider) is null
            )
            {
                key = $"{Guid.NewGuid()}:{Interlocked.Increment(ref _cookieSequenceGen)}";
                session = context.RequestServices.CreateScope().ServiceProvider;
                context.Response.Cookies.Append(_fullStateOptions.Cookie.Name, key, _fullStateOptions.Cookie.Build(context));
                isNewSession = true;
            }

            fullState.RequestServices = context.RequestServices;
            fullState.SessionServices = session!;
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
    /// <summary>
    /// <para xml:lang="ru">
    /// (Расширение)
    /// Возвращает текущую сессию
    /// </para>
    /// <para xml:lang="en">
    /// (Extension)
    /// Returns the current session
    /// </para>
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <returns></returns>
    public static IFullState GetFullState(this IServiceProvider serviceProvider)
    {
        IHttpContextAccessor ca = serviceProvider.GetRequiredService<IHttpContextAccessor>();
        return ca.HttpContext.RequestServices.GetRequiredService<IFullState>();
    }

}
