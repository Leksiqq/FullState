using Microsoft.AspNetCore.Http;

namespace Net.Leksi.FullState;
/// <summary>
/// <para xml:lang="ru">
/// Параметры для настройки full state сервера
/// </para>
/// <para xml:lang="en">
/// Parameters for configuring the full state server
/// </para>
/// </summary>
public class FullStateOptions
{
    /// <summary>
    /// <para xml:lang="ru">
    /// Указывает продолжительность бездействия сессии до того, как она будет прекращена. 
    /// Каждый доступ к сессии сбрасывает время ожидания
    /// </para>
    /// <para xml:lang="en">
    /// Indicates how long the session will be idle before it is terminated.
    /// Each session access resets the timeout
    /// </para>
    /// </summary>
    public TimeSpan IdleTimeout { get; set; } = TimeSpan.FromHours(1);
    /// <summary>
    /// <para xml:lang="ru">
    /// Указывает периодичность проверки завершённых сессий для освобождения их ресурсов
    /// </para>
    /// <para xml:lang="en">
    /// Specifies the frequency of checking for completed sessions to free their resources
    /// </para>
    /// </summary>
    public TimeSpan ExpirationScanFrequency { get; set; } = TimeSpan.FromSeconds(1);
    /// <summary>
    /// <para xml:lang="ru">
    /// Определяет параметры, используемые для создания файлов cookie. 
    /// 
    /// Для Name задается значение по умолчанию <see cref="FullStateDefaults.CookieName"/>. 
    /// Для Path задается значение по умолчанию <see cref="FullStateDefaults.CookiePath"/>. 
    /// Для SameSite задается значение по умолчанию <see cref="SameSiteMode.Lax"/>. 
    /// HttpOnlyпо умолчанию имеет значение <c>true</c>.
    /// IsEssential по умолчанию <c>false</c>
    /// </para>
    /// <para xml:lang="en">
    /// Defines the options used to create cookies.
    ///
    /// Name is set to the default value <see cref="FullStateDefaults.CookieName"/>.
    /// Path is set to the default <see cref="FullStateDefaults.CookiePath"/>.
    /// SameSite is set to the default <see cref="SameSiteMode.Lax"/>.
    /// HttpOnly is <c>true</c> by default.
    /// IsEssential by default <c>false</c>
    /// </para>
    /// </summary>
    public CookieBuilder Cookie { get; init; } = new()
    {
        Name = FullStateDefaults.CookieName,
        Path = FullStateDefaults.CookiePath,
        SameSite = SameSiteMode.Lax,
        IsEssential = false,
        HttpOnly = true,
    };
}
