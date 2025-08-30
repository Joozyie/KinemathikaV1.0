public class SupabaseTokenRefresh
{
    private readonly RequestDelegate _next;
    private readonly SupabaseAuth _supabase;
    private readonly SupabaseTokenManager _tokenManager;

    public SupabaseTokenRefresh(
        RequestDelegate next,
        SupabaseAuth supabase,
        SupabaseTokenManager tokenManager)
    {
        _next = next;
        _supabase = supabase;
        _tokenManager = tokenManager;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var accessToken = _tokenManager.GetToken("sb-access-token");
        var refreshToken = _tokenManager.GetToken("sb-refresh-token");

        if (!string.IsNullOrEmpty(accessToken))
        {
            _supabase.SetAccessToken(accessToken);

            if (_supabase.IsTokenExpired())
            {
                if (!string.IsNullOrEmpty(refreshToken))
                {
                    var newTokens = await _supabase.RefreshTokenAsync(refreshToken);
                    if (newTokens != null)
                    {
                        _tokenManager.SetAuthToken(newTokens);
                        _supabase.SetAccessToken(newTokens.AccessToken);
                    }
                }
            }
        }

        await _next(context);
    }
}
