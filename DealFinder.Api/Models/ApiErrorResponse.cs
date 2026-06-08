namespace DealFinder.Api.Models;

public class ApiErrorResponse
{
    public string Message { get; set; } = "";
    public string Code { get; set; } = "";
    public string TraceId { get; set; } = "";
}