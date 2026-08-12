namespace AstreeClaims.Api.Services.Email;

public sealed record OutgoingEmail(string To, string Subject, string BodyHtml, string BodyText);
public sealed record EmailDeliveryResult(string ProviderMessageId);

public interface IEmailSender
{
    Task<EmailDeliveryResult> SendAsync(OutgoingEmail email, CancellationToken cancellationToken = default);
}
