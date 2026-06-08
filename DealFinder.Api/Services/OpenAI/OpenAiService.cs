using System.Text;
using DealFinder.Api.Models.OpenAI;
using Newtonsoft.Json;

namespace DealFinder.Api.Services.OpenAI;

public class OpenAiService : IOpenAiService
{
    private const int MaxOutputTokens = 500;

    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OpenAiService> _logger;

    public OpenAiService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<OpenAiService> logger
    )
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<ListingEvaluationResult> EvaluateListingAsync(string prompt)
    {
        var apiKey = _configuration["OpenAI:ApiKey"];

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("OpenAI API key is not configured.");
        }

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "https://api.openai.com/v1/chat/completions"
        );

        request.Headers.Add("Authorization", $"Bearer {apiKey}");

        var body = new
        {
            model = "gpt-4o-mini",
            temperature = 0,
            max_tokens = MaxOutputTokens,
            response_format = new
            {
                type = "json_object"
            },
            messages = new object[]
            {
                new
                {
                    role = "system",
                    content = "Return only valid compact JSON. No markdown. No extra text."
                },
                new
                {
                    role = "user",
                    content = prompt
                }
            }
        };

        var json = JsonConvert.SerializeObject(body);

        request.Content = new StringContent(
            json,
            Encoding.UTF8,
            "application/json"
        );

        _logger.LogInformation(
            "Sending OpenAI listing evaluation request. PromptLength={PromptLength}, MaxOutputTokens={MaxOutputTokens}",
            prompt.Length,
            MaxOutputTokens
        );

        using var response = await _httpClient.SendAsync(request);

        var responseJson = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "OpenAI request failed. StatusCode={StatusCode}, Body={Body}",
                response.StatusCode,
                responseJson
            );

            response.EnsureSuccessStatusCode();
        }

        dynamic parsed = JsonConvert.DeserializeObject(responseJson)!;

        string modelOutput = parsed.choices[0].message.content;

        _logger.LogDebug(
            "OpenAI raw listing evaluation JSON: {ModelOutput}",
            modelOutput
        );

        var evaluation =
            JsonConvert.DeserializeObject<ListingEvaluationResult>(modelOutput);

        if (evaluation is null)
        {
            throw new InvalidOperationException(
                "OpenAI returned invalid listing evaluation JSON."
            );
        }

        _logger.LogInformation(
            "OpenAI listing evaluation parsed. Model={Model}, Risk={RiskScore}, Value={ValueScore}, Condition={ConditionScore}",
            evaluation.Model,
            evaluation.RiskScore,
            evaluation.ValueScore,
            evaluation.ConditionScore
        );

        return evaluation;
    }
}   