namespace DealFinder.Api.Services.Scoring;

public interface IQueryIntentParser
{
    SearchIntent Parse(string query);
}