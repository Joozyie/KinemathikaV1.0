using RestSharp;
using System.Text.Json;

public class SupabaseDevApiService
{
    private readonly RestClient _client;
    private readonly string _serviceRoleKey;

    public SupabaseDevApiService(string baseUrl, string serviceRoleKey)
    {
        _client = new RestClient(baseUrl);
        _serviceRoleKey = serviceRoleKey;
    }

    public async Task<List<T>> GetTableAsync<T>(string table, Dictionary<string, string>? queryParams = null)
    {
        var request = new RestRequest(table, Method.Get);
        request.AddHeader("apikey", _serviceRoleKey);
        request.AddHeader("Authorization", $"Bearer {_serviceRoleKey}");

        if (queryParams != null)
        {
            foreach (var kvp in queryParams)
            {
                request.AddQueryParameter(kvp.Key, kvp.Value);
            }
        }

        var response = await _client.ExecuteAsync(request);
        response.ThrowIfError();
        return JsonSerializer.Deserialize<List<T>>(response.Content ?? "[]") ?? new();
    }
}