using AstreeClaims.Api.DTOs.Claims;
using AstreeClaims.Api.DTOs.Common;

namespace AstreeClaims.Api.Services.Claims;

public interface IClaimsService
{
    Task<PagedResultDto<ClaimDto>> GetClaimsAsync(
        ClaimListQueryDto query,
        CancellationToken cancellationToken = default);

    Task<ClaimDto?> GetClaimAsync(
        string claimId,
        CancellationToken cancellationToken = default);

    Task<ClaimContextDto?> GetClaimContextAsync(
        string claimId,
        CancellationToken cancellationToken = default);
}
