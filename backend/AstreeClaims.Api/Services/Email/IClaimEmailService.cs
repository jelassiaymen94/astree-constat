using AstreeClaims.Api.DTOs.Email;

namespace AstreeClaims.Api.Services.Email;

public interface IClaimEmailService
{
    Task<ClaimEmailDto> SendAsync(string claimId, SendClaimEmailRequestDto request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ClaimEmailDto>> GetHistoryAsync(string claimId, CancellationToken cancellationToken = default);
}
