public class SupabaseTokenManager
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public SupabaseTokenManager(IHttpContextAccessor accessor)
    {
        _httpContextAccessor = accessor;
    }

    public void SetAuthToken(LoginResponse loginResponse)
    {
        var context = _httpContextAccessor.HttpContext!;

        // HttpOnly sensitive cookies
        context.Response.Cookies.Append("sb-access-token", loginResponse.AccessToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddHours(1)
        });
        context.Response.Cookies.Append("sb-refresh-token", loginResponse.RefreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddDays(7)
        });
    }

    public string? GetToken(string name)
    {
        var context = _httpContextAccessor.HttpContext!;
        return context.Request.Cookies[name];
    }

    public void ClearTokens()
    {
        var context = _httpContextAccessor.HttpContext!;

        // Expire cookies immediately
        context.Response.Cookies.Delete("sb-access-token");
        context.Response.Cookies.Delete("sb-refresh-token");
    }
}
