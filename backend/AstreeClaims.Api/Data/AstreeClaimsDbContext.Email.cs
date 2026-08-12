using AstreeClaims.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AstreeClaims.Api.Data;

public partial class AstreeClaimsDbContext
{
    public virtual DbSet<EmailLog> EmailLogs { get; set; }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Client>().Property(client => client.Email).HasMaxLength(254);

        modelBuilder.Entity<EmailLog>(entity =>
        {
            entity.HasKey(email => email.EmailId);
            entity.HasIndex(email => email.ClientRequestId).IsUnique();
            entity.HasIndex(email => new { email.ClaimId, email.CreatedAt }).IsDescending(false, true);
            entity.Property(email => email.EmailId).HasDefaultValueSql("(newid())");
            entity.Property(email => email.ClaimId).HasMaxLength(20);
            entity.Property(email => email.RecipientEmail).HasMaxLength(254);
            entity.Property(email => email.ActualRecipientEmail).HasMaxLength(254);
            entity.Property(email => email.Subject).HasMaxLength(200);
            entity.Property(email => email.Status).HasMaxLength(20);
            entity.Property(email => email.ProviderMessageId).HasMaxLength(200);
            entity.Property(email => email.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.HasOne(email => email.Claim)
                .WithMany(claim => claim.EmailLogs)
                .HasForeignKey(email => email.ClaimId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });
    }
}
