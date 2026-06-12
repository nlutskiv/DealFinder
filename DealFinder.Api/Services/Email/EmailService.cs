using System.Net.Http.Json;

namespace DealFinder.Api.Services.Email;

public class EmailService : IEmailService
{
    private readonly HttpClient _http;
    private readonly ILogger<EmailService> _logger;
    private readonly string _apiKey;
    private readonly string _fromAddress;

    public EmailService(HttpClient http, IConfiguration config, ILogger<EmailService> logger)
    {
        _http = http;
        _logger = logger;
        _apiKey = config["Resend:ApiKey"] ?? "";
        _fromAddress = config["Resend:FromAddress"] ?? "noreply@dealfinder.tech";
    }

    public async Task SendPriceAlertAsync(
        string toEmail,
        string productLabel,
        decimal targetPrice,
        decimal currentPrice,
        string dealUrl,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            _logger.LogWarning("Resend:ApiKey not configured — skipping email to {Email}", toEmail);
            return;
        }

        var html = $"""
            <h2 style="color:#00f5d4;font-family:sans-serif">Price drop alert 🎉</h2>
            <p style="font-family:sans-serif">
              <strong>{productLabel}</strong> is now available for
              <strong style="color:#00f5d4">£{currentPrice:F2}</strong>,
              which is below your target of £{targetPrice:F2}.
            </p>
            <p style="font-family:sans-serif">
              <a href="{dealUrl}" style="background:#00f5d4;color:#0a0a0e;padding:10px 20px;border-radius:8px;text-decoration:none;font-weight:bold">
                View Deal →
              </a>
            </p>
            <p style="font-family:sans-serif;font-size:0.8rem;color:#888">
              You received this because you set a price alert on <a href="https://www.dealfinder.tech">dealfinder.tech</a>.
            </p>
            """;

        var payload = new
        {
            from = _fromAddress,
            to = new[] { toEmail },
            subject = $"Price drop: {productLabel} is now £{currentPrice:F2}",
            html,
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.resend.com/emails");
        req.Headers.Add("Authorization", $"Bearer {_apiKey}");
        req.Content = JsonContent.Create(payload);

        try
        {
            var resp = await _http.SendAsync(req, ct);
            if (!resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync(ct);
                _logger.LogError("Resend API error {Status}: {Body}", resp.StatusCode, body);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Failed to send price alert email to {Email}", toEmail);
        }
    }
}
