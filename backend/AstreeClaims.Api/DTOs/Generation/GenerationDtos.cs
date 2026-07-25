using System.ComponentModel.DataAnnotations;
using AstreeClaims.Api.DTOs.Claims;

namespace AstreeClaims.Api.DTOs.Generation;

public sealed class GenerateClaimRequestDto
{
    [Required]
    [RegularExpression("^(summary|letter|response)$",
        ErrorMessage = "Le type doit être summary, letter ou response.")]
    public required string GenerationType { get; init; }

    [MaxLength(1000)]
    public string? UserInstruction { get; init; }
}

public sealed record AiGenerationRequestDto(
    string GenerationType,
    string? UserInstruction,
    ClaimContextDto Context);

public sealed record AiGenerationResponseDto(
    string Content,
    string ModelName,
    string PromptVersion,
    int DurationMs);

public sealed record GenerationDto(
    Guid GenerationId,
    string ClaimId,
    string GenerationType,
    string? UserInstruction,
    string? GeneratedContent,
    string? ModelName,
    string PromptVersion,
    bool Success,
    string? ErrorMessage,
    DateTime CreatedAt,
    int? DurationMs,
    bool RequiresHumanValidation = true);
