using AstreeClaims.Api.DTOs.Generation;

namespace AstreeClaims.Api.Services.Generation;

public interface IAiGenerationClient
{
    Task<AiGenerationResponseDto> GenerateAsync(
        AiGenerationRequestDto request,
        CancellationToken cancellationToken = default);
}
