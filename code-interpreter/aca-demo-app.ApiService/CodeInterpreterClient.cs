using Azure.Core;
using Azure.Identity;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

public sealed class CodeInterpreterClient
{
    private readonly HttpClient _http = new();
    private readonly TokenCredential _cred = new DefaultAzureCredential();
    private readonly string _execUrl;

    public CodeInterpreterClient(IConfiguration cfg)
    {
        var region   = cfg["Sessions:Region"];       
        var subId    = cfg["Sessions:SubscriptionId"];
        var rg       = cfg["Sessions:ResourceGroup"];
        var pool     = cfg["Sessions:PoolName"];      
        _execUrl     =
   $"https://{region}.dynamicsessions.io/" +
   $"subscriptions/{subId}/resourceGroups/{rg}/" +
   $"sessionPools/{pool}/code/execute?api-version=2024-02-02-preview";
    }

    public async Task<string> RunPythonAsync(string sessionId, string code)
    {
        var token = await _cred.GetTokenAsync(
            new TokenRequestContext(
                ["https://dynamicsessions.io/.default"]),CancellationToken.None);

        _http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token.Token);

        var body = new
        {
            properties = new
            {
                codeInputType  = "inline",
                executionType  = "synchronous",
                code
            }
        };

        var resp = await _http.PostAsJsonAsync(
                      $"{_execUrl}&identifier={sessionId}", body);
        resp.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        return doc.RootElement
                  .GetProperty("properties")
                  .GetProperty("stdout")
                  .GetString()!;
    }
}
