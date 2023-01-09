using System.Diagnostics;
using System.Net;
using System.Text.Json.Nodes;
using Polly;

namespace FluxGuardian.FluxApi.SDK;

public class FluxApiClient
{
    private readonly HttpClient HttpClient;
    private readonly string BaseUri;

    public FluxApiClient(string baseUri)
    {
        BaseUri = baseUri;
        HttpClient = new HttpClient();
        HttpClient.DefaultRequestHeaders.Add("User-Agent", "HeliumApi-SDK");
    }

    private async Task<HttpResponseMessage> PollyGet(Func<Task<HttpResponseMessage>> func)
    {
        return await Policy
            .Handle<HttpRequestException>()
            .Or<TaskCanceledException>()
            .OrResult<HttpResponseMessage>(TransientHttpFailures)
            .WaitAndRetryAsync(5, retryAttempt => TimeSpan.FromSeconds(3 * retryAttempt))
            .ExecuteAsync(func);
    }

    private bool TransientHttpFailures(HttpResponseMessage responseMessage)
    {
        return responseMessage.StatusCode == HttpStatusCode.TooManyRequests ||
               responseMessage.StatusCode == HttpStatusCode.RequestTimeout ||
               responseMessage.StatusCode == HttpStatusCode.BadGateway ||
               responseMessage.StatusCode == HttpStatusCode.GatewayTimeout ||
               responseMessage.StatusCode == HttpStatusCode.ServiceUnavailable;
    }

    public async Task<Response> Get(string relativePath)
    {
        var uri = BaseUri + relativePath;
        var responseMessage = await PollyGet(() =>
        {
            Debug.WriteLine($"{DateTime.Now:T} GET {uri}");
            return HttpClient.GetAsync(uri);
        });
        Debug.WriteLine($"Response status: {responseMessage.StatusCode}");
        responseMessage.EnsureSuccessStatusCode();
        var content = await responseMessage.Content.ReadAsStringAsync();

        JsonNode? result;
        try
        {
            result = JsonNode.Parse(content);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }

        return new Response
        {
            Status = result["status"].ToString(),
            Data = result["data"].ToJsonString()
        };
    }
}