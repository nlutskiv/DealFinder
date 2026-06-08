namespace DealFinder.Api.Exceptions;

public class AppException : Exception
{
    public int StatusCode { get; }
    public string Code { get; }

    public AppException(
        string message,
        string code,
        int statusCode = StatusCodes.Status400BadRequest
    ) : base(message)
    {
        Code = code;
        StatusCode = statusCode;
    }
}