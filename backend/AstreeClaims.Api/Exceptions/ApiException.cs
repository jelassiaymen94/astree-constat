namespace AstreeClaims.Api.Exceptions;

public abstract class ApiException : Exception
{
    protected ApiException(
        int statusCode,
        string code,
        string message,
        Exception? innerException = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
        Code = code;
    }

    public int StatusCode { get; }

    public string Code { get; }
}
