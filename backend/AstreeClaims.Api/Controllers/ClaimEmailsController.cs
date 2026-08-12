using AstreeClaims.Api.DTOs.Email;
using AstreeClaims.Api.Services.Email;
using Microsoft.AspNetCore.Mvc;

namespace AstreeClaims.Api.Controllers;

[ApiController]
[Route("api/claims/{claimId}/emails")]
public sealed class ClaimEmailsController : ControllerBase
{
    private readonly IClaimEmailService _service;
    public ClaimEmailsController(IClaimEmailService service) => _service = service;

    [HttpPost("send")]
    public async Task<ActionResult<ClaimEmailDto>> Send(string claimId, [FromBody] SendClaimEmailRequestDto request, CancellationToken cancellationToken) =>
        Ok(await _service.SendAsync(claimId, request, cancellationToken));

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ClaimEmailDto>>> History(string claimId, CancellationToken cancellationToken) =>
        Ok(await _service.GetHistoryAsync(claimId, cancellationToken));
}
