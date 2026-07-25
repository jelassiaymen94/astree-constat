using AstreeClaims.Api.DTOs.Claims;
using AstreeClaims.Api.DTOs.Common;
using AstreeClaims.Api.DTOs.Generation;
using AstreeClaims.Api.Exceptions;
using AstreeClaims.Api.Services.Claims;
using AstreeClaims.Api.Services.Generation;
using Microsoft.AspNetCore.Mvc;

namespace AstreeClaims.Api.Controllers;

[ApiController]
[Route("api/claims")]
public sealed class ClaimsController : ControllerBase
{
    private readonly IClaimsService _claimsService;
    private readonly IClaimGenerationService _generationService;

    public ClaimsController(IClaimsService claimsService, IClaimGenerationService generationService)
    {
        _claimsService = claimsService;
        _generationService = generationService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResultDto<ClaimDto>>> GetClaims([FromQuery] ClaimListQueryDto query, CancellationToken cancellationToken)
    {
        return Ok(await _claimsService.GetClaimsAsync(query, cancellationToken));
    }

    [HttpGet("{claimId}")]
    public async Task<ActionResult<ClaimDto>> GetClaim(string claimId, CancellationToken cancellationToken)
    {
        var normalizedClaimId = claimId.Trim();
        var claim = await _claimsService.GetClaimAsync(normalizedClaimId, cancellationToken);
        return claim is null ? throw new ClaimNotFoundException(normalizedClaimId) : Ok(claim);
    }

    [HttpGet("{claimId}/context")]
    public async Task<ActionResult<ClaimContextDto>> GetClaimContext(string claimId, CancellationToken cancellationToken)
    {
        var normalizedClaimId = claimId.Trim();
        var context = await _claimsService.GetClaimContextAsync(normalizedClaimId, cancellationToken);
        return context is null ? throw new ClaimNotFoundException(normalizedClaimId) : Ok(context);
    }

    [HttpPost("{claimId}/generate")]
    [ProducesResponseType(typeof(GenerationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorDto), StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<GenerationDto>> Generate(string claimId, [FromBody] GenerateClaimRequestDto request, CancellationToken cancellationToken)
    {
        return Ok(await _generationService.GenerateAsync(claimId, request, cancellationToken));
    }

    [HttpGet("{claimId}/generations")]
    public async Task<ActionResult<IReadOnlyList<GenerationDto>>> GetGenerations(string claimId, CancellationToken cancellationToken)
    {
        return Ok(await _generationService.GetHistoryAsync(claimId, cancellationToken));
    }
}
