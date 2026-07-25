using AstreeClaims.Api.DTOs.Claims;
using AstreeClaims.Api.DTOs.Common;
using AstreeClaims.Api.Exceptions;
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
    [ProducesResponseType(typeof(ApiErrorDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorDto), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<PagedResultDto<ClaimDto>>> GetClaims(
        [FromQuery] ClaimListQueryDto query,
        CancellationToken cancellationToken)
    {
        var result = await _claimsService.GetClaimsAsync(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{claimId}")]
    [ProducesResponseType(typeof(ClaimDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorDto), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<ClaimDto>> GetClaim(
        string claimId,
        CancellationToken cancellationToken)
    {
        var normalizedClaimId = claimId.Trim();
        var claim = await _claimsService.GetClaimAsync(
            normalizedClaimId,
            cancellationToken);

        return claim is null
            ? throw new ClaimNotFoundException(normalizedClaimId)
            : Ok(claim);
    }

    [HttpGet("{claimId}/context")]
    [ProducesResponseType(typeof(ClaimContextDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorDto), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<ClaimContextDto>> GetClaimContext(
        string claimId,
        CancellationToken cancellationToken)
    {
        var normalizedClaimId = claimId.Trim();
        var context = await _claimsService.GetClaimContextAsync(
            normalizedClaimId,
            cancellationToken);

        return context is null
            ? throw new ClaimNotFoundException(normalizedClaimId)
            : Ok(context);
    }
}
