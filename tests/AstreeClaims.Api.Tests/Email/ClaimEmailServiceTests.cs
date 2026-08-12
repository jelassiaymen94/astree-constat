using AstreeClaims.Api.DTOs.Email;
using AstreeClaims.Api.Services.Email;
using AstreeClaims.Api.Tests.Fixtures;

namespace AstreeClaims.Api.Tests.Email;

public sealed class ClaimEmailServiceTests
{
    [Fact]
    public async Task Send_records_a_successful_demo_email()
    {
        await using var context = await SqliteTestContext.CreateAsync();
        var sender = new FakeEmailSender();
        var service = new ClaimEmailService(context.Db, sender);
        var request = new SendClaimEmailRequestDto
        {
            ClientRequestId = Guid.NewGuid(), GenerationId = null,
            Subject = "Suivi du dossier", BodyHtml = "<p>Bonjour <strong>client</strong>.</p>", Confirmation = true
        };

        var result = await service.SendAsync("CLM-1001", request);

        Assert.Equal("sent", result.Status);
        Assert.EndsWith("@demo.astree.local", result.RecipientEmail);
        Assert.Single(context.Db.EmailLogs);
        Assert.Equal("Suivi du dossier", sender.LastEmail?.Subject);
    }

    [Fact]
    public async Task Send_removes_dangerous_html_before_delivery()
    {
        await using var context = await SqliteTestContext.CreateAsync();
        var sender = new FakeEmailSender();
        var service = new ClaimEmailService(context.Db, sender);
        var request = new SendClaimEmailRequestDto
        {
            ClientRequestId = Guid.NewGuid(), Subject = "Test",
            BodyHtml = "<p onclick=\"alert(1)\">Bonjour</p><script>alert(1)</script>", Confirmation = true
        };

        await service.SendAsync("CLM-1001", request);

        Assert.DoesNotContain("onclick", sender.LastEmail?.BodyHtml);
        Assert.DoesNotContain("script", sender.LastEmail?.BodyHtml);
    }

    private sealed class FakeEmailSender : IEmailSender
    {
        public OutgoingEmail? LastEmail { get; private set; }
        public Task<EmailDeliveryResult> SendAsync(OutgoingEmail email, CancellationToken cancellationToken = default)
        {
            LastEmail = email;
            return Task.FromResult(new EmailDeliveryResult("mailtrap-test-id"));
        }
    }
}
