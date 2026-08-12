using System.ComponentModel.DataAnnotations;

namespace AstreeClaims.Api.DTOs.Email;

public sealed class SendClaimEmailRequestDto
{
    [Required] public Guid ClientRequestId { get; init; }
    public Guid? GenerationId { get; init; }
    [Required, MaxLength(200)] public required string Subject { get; init; }
    [Required, MaxLength(50000)] public required string BodyHtml { get; init; }
    [Range(typeof(bool), "true", "true", ErrorMessage = "Une confirmation explicite est obligatoire.")]
    public bool Confirmation { get; init; }
}

public sealed record ClaimEmailDto(
    Guid EmailId,
    Guid ClientRequestId,
    string ClaimId,
    Guid? GenerationId,
    string RecipientEmail,
    string ActualRecipientEmail,
    string Subject,
    string BodyHtml,
    string Status,
    string? ProviderMessageId,
    string? ErrorMessage,
    DateTime CreatedAt,
    DateTime? SentAt,
    bool DemoMode);
