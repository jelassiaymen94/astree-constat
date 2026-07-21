using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace AstreeClaims.Api.DTOs.Claims;

public sealed class ClaimListQueryDto
{
    [Range(1, 100000)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 20;

    [MaxLength(50)]
    public string? Status { get; init; }

    [FromQuery(Name = "type")]
    [MaxLength(100)]
    public string? Type { get; init; }

    [MaxLength(20)]
    public string? Search { get; init; }
}
