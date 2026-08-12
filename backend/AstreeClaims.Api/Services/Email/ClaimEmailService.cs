using System.Net;
using System.Net.Mail;
using System.Text.RegularExpressions;
using AstreeClaims.Api.Data;
using AstreeClaims.Api.DTOs.Email;
using AstreeClaims.Api.Exceptions;
using AstreeClaims.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AstreeClaims.Api.Services.Email;

public sealed partial class ClaimEmailService : IClaimEmailService
{
    private readonly AstreeClaimsDbContext _db;
    private readonly IEmailSender _sender;
    private readonly bool _demoMode = !string.Equals(Environment.GetEnvironmentVariable("EMAIL_DEMO_MODE"), "false", StringComparison.OrdinalIgnoreCase);
    private readonly string? _demoRecipient = Environment.GetEnvironmentVariable("EMAIL_DEMO_RECIPIENT");

    public ClaimEmailService(AstreeClaimsDbContext db, IEmailSender sender) { _db = db; _sender = sender; }

    public async Task<ClaimEmailDto> SendAsync(string claimId, SendClaimEmailRequestDto request, CancellationToken cancellationToken = default)
    {
        var normalizedId = claimId.Trim();
        var existing = await _db.EmailLogs.AsNoTracking().SingleOrDefaultAsync(x => x.ClientRequestId == request.ClientRequestId, cancellationToken);
        if (existing is not null) return Map(existing);

        var claim = await _db.Sinistres.AsNoTracking().Include(x => x.Client).SingleOrDefaultAsync(x => x.ClaimId == normalizedId, cancellationToken)
            ?? throw new ClaimNotFoundException(normalizedId);

        var recipient = claim.Client.Email ?? $"{claim.Client.ClientId.ToLowerInvariant()}@demo.astree.local";
        var actualRecipient = _demoMode && !string.IsNullOrWhiteSpace(_demoRecipient) ? _demoRecipient.Trim() : recipient;
        _ = new MailAddress(actualRecipient);
        var safeHtml = SanitizeHtml(request.BodyHtml);
        var text = WebUtility.HtmlDecode(TagsRegex().Replace(safeHtml, " "));
        text = WhitespaceRegex().Replace(text, " ").Trim();

        var log = new EmailLog
        {
            EmailId = Guid.NewGuid(), ClientRequestId = request.ClientRequestId, ClaimId = normalizedId,
            GenerationId = request.GenerationId, RecipientEmail = recipient, ActualRecipientEmail = actualRecipient,
            Subject = request.Subject.Trim(), BodyHtml = safeHtml, BodyText = text, Status = "pending", CreatedAt = DateTime.UtcNow
        };
        _db.EmailLogs.Add(log);
        await _db.SaveChangesAsync(cancellationToken);

        try
        {
            var result = await _sender.SendAsync(new OutgoingEmail(actualRecipient, log.Subject, safeHtml, text), cancellationToken);
            log.Status = "sent"; log.ProviderMessageId = result.ProviderMessageId; log.SentAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            return Map(log);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            log.Status = "failed"; log.ErrorMessage = "L’envoi via Mailtrap a échoué.";
            await _db.SaveChangesAsync(cancellationToken);
            throw new EmailDeliveryException(exception);
        }
    }

    public async Task<IReadOnlyList<ClaimEmailDto>> GetHistoryAsync(string claimId, CancellationToken cancellationToken = default)
    {
        if (!await _db.Sinistres.AsNoTracking().AnyAsync(x => x.ClaimId == claimId, cancellationToken)) throw new ClaimNotFoundException(claimId);
        return await _db.EmailLogs.AsNoTracking().Where(x => x.ClaimId == claimId).OrderByDescending(x => x.CreatedAt).Select(x => Map(x)).ToListAsync(cancellationToken);
    }

    private ClaimEmailDto Map(EmailLog x) => new(x.EmailId, x.ClientRequestId, x.ClaimId, x.GenerationId, x.RecipientEmail, x.ActualRecipientEmail, x.Subject, x.BodyHtml, x.Status, x.ProviderMessageId, x.ErrorMessage, x.CreatedAt, x.SentAt, _demoMode);

    private static string SanitizeHtml(string html)
    {
        var value = DangerousBlocksRegex().Replace(html, "");
        value = EventAttributesRegex().Replace(value, "");
        value = JavascriptUrlsRegex().Replace(value, "");
        return value.Trim();
    }

    [GeneratedRegex(@"<(script|style|iframe|object|embed)[^>]*>.*?</\1>", RegexOptions.IgnoreCase | RegexOptions.Singleline)] private static partial Regex DangerousBlocksRegex();
    [GeneratedRegex(@"\s+on\w+\s*=\s*(['\"]).*?\1", RegexOptions.IgnoreCase | RegexOptions.Singleline)] private static partial Regex EventAttributesRegex();
    [GeneratedRegex(@"javascript\s*:", RegexOptions.IgnoreCase)] private static partial Regex JavascriptUrlsRegex();
    [GeneratedRegex("<[^>]+>")] private static partial Regex TagsRegex();
    [GeneratedRegex(@"\s+")] private static partial Regex WhitespaceRegex();
}
