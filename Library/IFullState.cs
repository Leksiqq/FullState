namespace Net.Leksi.FullState;

/// <summary>
/// <para xml:lang="ru">
/// Интерфейс для доступа к контейнеру DI Http-контекста из объектов со временем жизни сессии.
/// Извлекается из текущего scope контейнера DI
/// </para>
/// <para xml:lang="en">
/// Interface for accessing the Http context's DI container from objects with session lifetime.
/// Retrieved from the current scope of the DI container
/// </para>
/// <example>
/// <code>
/// public class InfoProvider
/// {
///     private readonly IServiceProvider _serviceProvider;
/// 
///     public InfoProvider(IServiceProvider serviceProvider)
///     {
///         _serviceProvider = serviceProvider;
///         ...
///     }
///    
///     ...
///
///     public string Get()
///     {
///         IFullState session = _serviceProvider.GetRequiredService{IFullState}();
///         Another another = session.RequestServices.GetRequiredService{Another}();
///         ...
///     }
///     ...
/// }
/// </code>
/// </example>
/// </summary>
public interface IFullState
{
    /// <summary>
    /// <para xml:lang="ru">
    /// Свойство, содержащее контейнер DI Http-контекста
    /// </para>
    /// <para xml:lang="en">
    /// Property containing the Http context's DI container
    /// </para>
    /// </summary>
    IServiceProvider RequestServices { get; }
}
