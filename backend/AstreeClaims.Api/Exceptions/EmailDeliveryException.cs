namespace AstreeClaims.Api.Exceptions;

public sealed class EmailDeliveryException : ApiException
{
    public EmailDeliveryException(Exception innerException) : base(
        StatusCodes.Status502BadGateway,
        "EMAIL_DELIVERY_FAILED",
        "L’e-mail n’a pas pu être envoyé via le service de démonstration.",
        innerException) { }
}
