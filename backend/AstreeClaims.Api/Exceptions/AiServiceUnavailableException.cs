namespace AstreeClaims.Api.Exceptions;

public sealed class AiServiceUnavailableException : ApiException
{
    public AiServiceUnavailableException(Exception innerException)
        : base(
            StatusCodes.Status502BadGateway,
            "AI_SERVICE_UNAVAILABLE",
            "Le service IA est temporairement indisponible.",
            innerException)
    {
    }
}
