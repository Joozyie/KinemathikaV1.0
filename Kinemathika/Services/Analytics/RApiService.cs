using RestSharp;
using System.Text.Json;
using System.Threading.Tasks;

public class RApiService
{
    private readonly RestClient _client;
    private readonly JsonSerializerOptions _jsonOptions;

    public RApiService(string baseUrl)
    {
        _client = new RestClient(baseUrl);
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };
    }

    public async Task<T> PostAsync<T>(string endpoint, object data)
    {
        var request = new RestRequest(endpoint, Method.Post);
        request.AddJsonBody(data);

        var response = await _client.ExecuteAsync(request);

        // Log raw response for debugging, can be removed in production
        Console.WriteLine("=== Raw R API Response ===");
        Console.WriteLine(response.Content);

        if (!response.IsSuccessful)
        {
            throw new Exception($"R API error: Status {response.StatusCode}, Body: {response.Content}");
        }

        if (string.IsNullOrWhiteSpace(response.Content))
        {
            throw new Exception("R API returned empty response.");
        }

        try
        {
            return JsonSerializer.Deserialize<T>(response.Content, _jsonOptions)
                ?? throw new Exception("Failed to deserialize R API response.");
        }
        catch (JsonException ex)
        {
            throw new Exception($"JSON Deserialization Error: {ex.Message}\nRaw Content: {response.Content}");
        }
    }
}
