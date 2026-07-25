namespace AstreeClaims.Api.DTOs.Common;

public sealed record ApiErrorDto(
    string Code,
    string Message,
    string TraceId,
    IReadOnlyDictionary<string, string[]>? Errors = null);
