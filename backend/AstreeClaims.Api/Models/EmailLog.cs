namespace AstreeClaims.Api.Models;

public sealed class EmailLog
{
    public Guid EmailId { get; set; }
    public Guid ClientRequestId { get; set; }
    public string ClaimId { get; set; } = null!;
    public Guid? GenerationId { get; set; }
    public string RecipientEmail { get; set; } = null!;
    public string ActualRecipientEmail { get; set; } = null!;
    public string Subject { get; set; } = null!;
    public string BodyHtml { get; set; } = null!;
    public string BodyText { get; set; } = null!;
    public string Status { get; set; } = "pending";
    public string? ProviderMessageId { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? SentAt { get; set; }
    public Sinistre Claim { get; set; } = null!;
}
