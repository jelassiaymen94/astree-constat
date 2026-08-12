using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;

namespace AstreeClaims.Api.Services.Email;

public sealed class SmtpEmailSender : IEmailSender
{
    private readonly string? _host = Environment.GetEnvironmentVariable("MAILTRAP_SMTP_HOST");
    private readonly int _port = int.TryParse(Environment.GetEnvironmentVariable("MAILTRAP_SMTP_PORT"), out var port) ? port : 2525;
    private readonly string? _username = Environment.GetEnvironmentVariable("MAILTRAP_SMTP_USERNAME");
    private readonly string? _password = Environment.GetEnvironmentVariable("MAILTRAP_SMTP_PASSWORD");
    private readonly string _fromAddress = Environment.GetEnvironmentVariable("EMAIL_FROM_ADDRESS") ?? "sinistres-demo@astree.local";
    private readonly string _fromName = Environment.GetEnvironmentVariable("EMAIL_FROM_NAME") ?? "ASTREE Assurances — Démonstration";

    public async Task<EmailDeliveryResult> SendAsync(OutgoingEmail email, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_host) || string.IsNullOrWhiteSpace(_username) || string.IsNullOrWhiteSpace(_password))
            throw new InvalidOperationException("La configuration SMTP Mailtrap est incomplète.");

        cancellationToken.ThrowIfCancellationRequested();
        using var message = new MailMessage
        {
            From = new MailAddress(_fromAddress, _fromName, Encoding.UTF8),
            Subject = email.Subject,
            SubjectEncoding = Encoding.UTF8,
            BodyEncoding = Encoding.UTF8,
            HeadersEncoding = Encoding.UTF8,
            Body = email.BodyText,
            IsBodyHtml = false
        };
        message.To.Add(new MailAddress(email.To));

        // Multipart/alternative explicite : Mailtrap peut afficher séparément
        // la version texte et le rendu HTML.
        var textView = AlternateView.CreateAlternateViewFromString(
            email.BodyText,
            Encoding.UTF8,
            MediaTypeNames.Text.Plain);
        var htmlView = AlternateView.CreateAlternateViewFromString(
            email.BodyHtml,
            Encoding.UTF8,
            MediaTypeNames.Text.Html);
        message.AlternateViews.Add(textView);
        message.AlternateViews.Add(htmlView);

        using var smtp = new SmtpClient(_host, _port)
        {
            EnableSsl = true,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(_username, _password),
            DeliveryMethod = SmtpDeliveryMethod.Network
        };
        await smtp.SendMailAsync(message);
        return new EmailDeliveryResult(message.Headers["Message-Id"] ?? Guid.NewGuid().ToString("N"));
    }
}
