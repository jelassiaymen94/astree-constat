using AstreeClaims.Api.DTOs.Generation;
using AstreeClaims.Api.Exceptions;
using AstreeClaims.Api.Services.Generation;

namespace AstreeClaims.Api.Tests.Fixtures;

internal sealed class FakeAiGenerationClient : IAiGenerationClient
{
    public Task<AiGenerationResponseDto> GenerateAsync(AiGenerationRequestDto request, CancellationToken cancellationToken = default)
    {
        if (request.UserInstruction == "simulate-unavailable")
            throw new AiServiceUnavailableException(new HttpRequestException("Simulated FastAPI outage."));
        return Task.FromResult(new AiGenerationResponseDto($"Brouillon {request.GenerationType} pour {request.Context.Claim.ClaimId}", "fake-test-model", "test-1.0", 5));
    }
}
