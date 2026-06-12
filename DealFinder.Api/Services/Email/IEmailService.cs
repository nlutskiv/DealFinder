namespace DealFinder.Api.Services.Email;

public interface IEmailService
{
    Task SendPriceAlertAsync(string toEmail, string productLabel, decimal targetPrice, decimal currentPrice, string dealUrl, CancellationToken ct = default);
}
