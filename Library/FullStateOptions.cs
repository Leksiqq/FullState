using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Session;

namespace Net.Leksi.Server;

public class FullStateOptions
{
    public TimeSpan IdleTimeout { get; set; } = TimeSpan.FromHours(1);
    public TimeSpan ExpirationScanFrequency { get; set; } = TimeSpan.FromSeconds(1);
    public CookieBuilder Cookie { get; set; } = new()
    {
        Name = SessionDefaults.CookieName,
        Path = SessionDefaults.CookiePath,
        SameSite = SameSiteMode.Lax,
        IsEssential = false,
        HttpOnly = true,
    };
}
