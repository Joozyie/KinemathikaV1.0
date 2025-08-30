using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

public class LoginResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = "";

    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = "";

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    [JsonPropertyName("expires_at")]
    public long ExpiresAt { get; set; }

    [JsonPropertyName("refresh_token")]
    public string RefreshToken { get; set; } = "";

    [JsonPropertyName("user")]
    public SupabaseUser User { get; set; } = new();
}

public class SupabaseUser
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("aud")]
    public string Audience { get; set; } = "";

    [JsonPropertyName("role")]
    public string Role { get; set; } = "";

    [JsonPropertyName("email")]
    public string Email { get; set; } = "";

    [JsonPropertyName("email_confirmed_at")]
    public DateTime? EmailConfirmedAt { get; set; }

    [JsonPropertyName("phone")]
    public string Phone { get; set; } = "";

    [JsonPropertyName("confirmed_at")]
    public DateTime? ConfirmedAt { get; set; }

    [JsonPropertyName("last_sign_in_at")]
    public DateTime? LastSignInAt { get; set; }

    [JsonPropertyName("app_metadata")]
    public Dictionary<string, object> AppMetadata { get; set; } = new();

    [JsonPropertyName("user_metadata")]
    public Dictionary<string, object> UserMetadata { get; set; } = new();

    [JsonPropertyName("identities")]
    public List<SupabaseIdentity> Identities { get; set; } = new();

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [JsonPropertyName("is_anonymous")]
    public bool IsAnonymous { get; set; }
}

public class SupabaseIdentity
{
    [JsonPropertyName("identity_id")]
    public string IdentityId { get; set; } = "";

    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("user_id")]
    public string UserId { get; set; } = "";

    [JsonPropertyName("identity_data")]
    public Dictionary<string, object> IdentityData { get; set; } = new();

    [JsonPropertyName("provider")]
    public string Provider { get; set; } = "";

    [JsonPropertyName("last_sign_in_at")]
    public DateTime? LastSignInAt { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [JsonPropertyName("email")]
    public string Email { get; set; } = "";
}
