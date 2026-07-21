namespace AstreeClaims.Api.DTOs.Claims;

public sealed record ClaimDto(
    string ClaimId,
    DateOnly Date,
    string Type,
    string Description,
    decimal EstimatedAmount,
    decimal CompensationAmount,
    string Status);

public sealed record CustomerDto(
    string ClientId,
    string FirstName,
    string LastName,
    string Governorate);

public sealed record ContractDto(
    string ContractId,
    string CoverageType,
    DateOnly StartDate,
    DateOnly EndDate);

public sealed record VehicleDto(
    string VehicleId,
    string Type,
    string Brand,
    string Model,
    string RegistrationNumber);

public sealed record ClaimContextDto(
    ClaimDto Claim,
    CustomerDto Customer,
    ContractDto Contract,
    VehicleDto Vehicle);
