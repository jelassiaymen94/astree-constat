using AstreeClaims.Api.DTOs.Import;

namespace AstreeClaims.Api.Services.Import;

public interface IDataImportService
{
    Task<ImportResultDto> ImportAsync(
        string sourceDirectory,
        CancellationToken cancellationToken = default);
}
