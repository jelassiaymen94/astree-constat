namespace AstreeClaims.Api.DTOs.Import;

public sealed record ImportTableResultDto(
    int SourceRows,
    int InsertedRows,
    int SkippedRows);

public sealed record ImportResultDto(
    string SourceDirectory,
    ImportTableResultDto Clients,
    ImportTableResultDto Contrats,
    ImportTableResultDto Vehicules,
    ImportTableResultDto Sinistres,
    DateTime StartedAtUtc,
    DateTime CompletedAtUtc);
