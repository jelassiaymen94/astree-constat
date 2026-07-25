using AstreeClaims.Api.DTOs.Generation;

namespace AstreeClaims.Api.Services.Generation;

public interface IClaimGenerationService
{
    Task<GenerationDto> GenerateAsync(
        string claimId,
        GenerateClaimRequestDto request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GenerationDto>> GetHistoryAsync(
        string claimId,
        CancellationToken cancellationToken = default);
}
