using AstreeClaims.Api.DTOs.Claims;
using AstreeClaims.Api.DTOs.Common;
using AstreeClaims.Api.Services.Claims;
using Microsoft.AspNetCore.Mvc;

namespace AstreeClaims.Api.Controllers;

[ApiController]
[Route("api/claims")]
public sealed class ClaimsController : ControllerBase
{
    private readonly IClaimsService _claimsService;

    public ClaimsController(IClaimsService claimsService)
    {
        _claimsService = claimsService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResultDto<ClaimDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResultDto<ClaimDto>>> GetClaims(
        [FromQuery] ClaimListQueryDto query,
        CancellationToken cancellationToken)
    {
        var result = await _claimsService.GetClaimsAsync(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{claimId}")]
    [ProducesResponseType(typeof(ClaimDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClaimDto>> GetClaim(
        string claimId,
        CancellationToken cancellationToken)
    {
        var normalizedClaimId = claimId.Trim();
        var claim = await _claimsService.GetClaimAsync(
            normalizedClaimId,
            cancellationToken);

        if (claim is null)
        {
            return NotFound(new
            {
                code = "CLAIM_NOT_FOUND",
                message = $"Le sinistre {normalizedClaimId} est introuvable."
            });
        }

        return Ok(claim);
    }

    [HttpGet("{claimId}/context")]
    [ProducesResponseType(typeof(ClaimContextDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClaimContextDto>> GetClaimContext(
        string claimId,
        CancellationToken cancellationToken)
    {
        var normalizedClaimId = claimId.Trim();
        var context = await _claimsService.GetClaimContextAsync(
            normalizedClaimId,
            cancellationToken);

        if (context is null)
        {
            return NotFound(new
            {
                code = "CLAIM_NOT_FOUND",
                message = $"Le sinistre {normalizedClaimId} est introuvable."
            });
        }

        return Ok(context);
    }
}
