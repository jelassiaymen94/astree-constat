namespace AstreeClaims.Api.Models;

public partial class Sinistre
{
    public virtual ICollection<EmailLog> EmailLogs { get; set; } = new List<EmailLog>();
}
