using System.Text;
using Newtonsoft.Json;
using DealFinder.Api.Models.OpenAI;

namespace DealFinder.Api.Services.OpenAI;

public class OpenAiService : IOpenAiService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public OpenAiService(
        HttpClient httpClient,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<GpuEvaluationResult> EvaluateGpuListingAsync(string prompt)
    {
        var apiKey = _configuration["OpenAI:ApiKey"];

        _httpClient.DefaultRequestHeaders.Clear();

        _httpClient.DefaultRequestHeaders.Add(
            "Authorization",
            $"Bearer {apiKey}"
        );

        var body = new
        {
            model = "gpt-4o-mini",
            response_format = new
            {
                type = "json_object"
            },
            messages = new object[]
            {
                new
                {
                    role = "system",
                    content = "Return only valid JSON."
                },
                new
                {
                    role = "user",
                    content = prompt
                }
            }
        };

        var json = JsonConvert.SerializeObject(body);

        var content = new StringContent(
            json,
            Encoding.UTF8,
            "application/json"
        );

        var response = await _httpClient.PostAsync(
            "https://api.openai.com/v1/chat/completions",
            content
        );

        response.EnsureSuccessStatusCode();

        var responseJson =
            await response.Content.ReadAsStringAsync();

        dynamic parsed =
            JsonConvert.DeserializeObject(responseJson)!;

        string modelOutput =
            parsed.choices[0].message.content;

        return JsonConvert.DeserializeObject<GpuEvaluationResult>(
            modelOutput
        )!;
    }
}