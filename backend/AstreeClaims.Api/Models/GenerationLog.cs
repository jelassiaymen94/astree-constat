using System;
using System.Collections.Generic;

namespace AstreeClaims.Api.Models;

public partial class GenerationLog
{
    public Guid GenerationId { get; set; }

    public string ClaimId { get; set; } = null!;

    public string GenerationType { get; set; } = null!;

    public string? UserInstruction { get; set; }

    public string? GeneratedContent { get; set; }

    public string? ModelName { get; set; }

    public string PromptVersion { get; set; } = null!;

    public bool Success { get; set; }

    public string? ErrorMessage { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? DurationMs { get; set; }

    public virtual Sinistre Claim { get; set; } = null!;
}
