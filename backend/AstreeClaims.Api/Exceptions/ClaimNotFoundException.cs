namespace AstreeClaims.Api.Exceptions;

public sealed class ClaimNotFoundException : ApiException
{
    public ClaimNotFoundException(string claimId)
        : base(
            StatusCodes.Status404NotFound,
            "CLAIM_NOT_FOUND",
            $"Le sinistre {claimId} est introuvable.")
    {
    }
}
