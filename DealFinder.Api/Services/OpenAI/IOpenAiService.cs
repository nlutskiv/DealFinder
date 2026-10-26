namespace DealFinder.Api.Services.OpenAI;

public interface IOpenAiService
{
    Task<string> CompleteChatAsync(string prompt, int maxTokens = 1000);
}