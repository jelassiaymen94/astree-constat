namespace AstreeClaims.Api.DTOs.Common;

public sealed record PagedResultDto<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int Total);
