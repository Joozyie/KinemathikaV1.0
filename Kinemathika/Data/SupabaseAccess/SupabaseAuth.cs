using RestSharp;
using System.Text.Json;

public class SupabaseAuth
{
    private readonly RestClient _client;
    private readonly string _anonKey;
    private string accessToken = "";

    public SupabaseAuth(string baseUrl, string anonKey)
    {
        _client = new RestClient(baseUrl);
        _anonKey = anonKey;
    }

    public void SetAccessToken(string token)
    {
        accessToken = token;
    }

    // Auth Operations ====================================

    // Login
    public async Task<LoginResponse?> SignInAsync(string email, string password)
    {
        var request = new RestRequest("auth/v1/token?grant_type=password", Method.Post);
        request.AddHeader("apikey", _anonKey);
        request.AddHeader("Content-Type", "application/json");
        request.AddJsonBody(new { email, password });

        var response = await _client.ExecuteAsync<LoginResponse>(request);
        if (!response.IsSuccessful)
        {
            return null;
        }
        return response.Data;
    }

    // Logout
    public async Task LogoutAsync()
    {
        var request = new RestRequest("auth/v1/logout", Method.Post);
        request.AddHeader("apikey", _anonKey);
        request.AddHeader("Authorization", $"Bearer {accessToken}");

        var response = await _client.ExecuteAsync(request);
        response.ThrowIfError();
    }

    // Check for token expiration
    public bool IsTokenExpired()
    {
        if (string.IsNullOrEmpty(accessToken)) return true;

        try
        {
            var parts = accessToken.Split('.');
            if (parts.Length != 3) return true; // simple check for malformed token

            var payload = parts[1];
            // Add padding if missing
            switch (payload.Length % 4)
            {
                case 2: payload += "=="; break;
                case 3: payload += "="; break;
            }
            var bytes = Convert.FromBase64String(payload);
            var json = System.Text.Encoding.UTF8.GetString(bytes);
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("exp", out var expElem) && expElem.TryGetInt64(out var exp))
            {
                var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                return now >= exp;
            }
            return true;
        }
        catch
        {
            return true;
        }
    }

    // Refresh Token
    public async Task<LoginResponse?> RefreshTokenAsync(string refreshToken)
    {
        var request = new RestRequest("auth/v1/token?grant_type=refresh_token", Method.Post);
        request.AddHeader("apikey", _anonKey);
        request.AddHeader("Content-Type", "application/json");
        request.AddJsonBody(new { refresh_token = refreshToken });

        var response = await _client.ExecuteAsync<LoginResponse>(request);
        if (!response.IsSuccessful)
            return null;
        return response.Data;
    }
}
