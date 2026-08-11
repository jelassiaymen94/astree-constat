using AstreeClaims.Api.Data;
using AstreeClaims.Api.DTOs.Claims;
using AstreeClaims.Api.DTOs.Common;
using Microsoft.EntityFrameworkCore;

namespace AstreeClaims.Api.Services.Claims;

public sealed class ClaimsService : IClaimsService
{
    private readonly AstreeClaimsDbContext _dbContext;

    public ClaimsService(AstreeClaimsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResultDto<ClaimDto>> GetClaimsAsync(
        ClaimListQueryDto query,
        CancellationToken cancellationToken = default)
    {
        var claimsQuery = _dbContext.Sinistres.AsNoTracking();

        var status = query.Status?.Trim();
        if (!string.IsNullOrEmpty(status))
        {
            claimsQuery = claimsQuery.Where(claim => claim.Statut == status);
        }

        var type = query.Type?.Trim();
        if (!string.IsNullOrEmpty(type))
        {
            claimsQuery = claimsQuery.Where(claim => claim.TypeSinistre == type);
        }

        var search = query.Search?.Trim();
        if (!string.IsNullOrEmpty(search))
        {
            // La même barre recherche une référence ou le nom de l'assuré,
            // dans l'ordre « prénom nom » ou « nom prénom ».
            claimsQuery = claimsQuery.Where(claim =>
                claim.ClaimId.Contains(search) ||
                (claim.Client.Prenom + " " + claim.Client.Nom).Contains(search) ||
                (claim.Client.Nom + " " + claim.Client.Prenom).Contains(search));
        }

        var total = await claimsQuery.CountAsync(cancellationToken);
        var skip = checked((query.Page - 1) * query.PageSize);

        var items = await claimsQuery
            .OrderByDescending(claim => claim.DateSinistre)
            .ThenBy(claim => claim.ClaimId)
            .Skip(skip)
            .Take(query.PageSize)
            .Select(claim => new ClaimDto(
                claim.ClaimId,
                claim.DateSinistre,
                claim.TypeSinistre,
                claim.Description,
                claim.MontantEstime,
                claim.MontantIndemnisation,
                claim.Statut))
            .ToListAsync(cancellationToken);

        return new PagedResultDto<ClaimDto>(items, query.Page, query.PageSize, total);
    }

    public Task<ClaimDto?> GetClaimAsync(
        string claimId,
        CancellationToken cancellationToken = default) =>
        _dbContext.Sinistres
            .AsNoTracking()
            .Where(claim => claim.ClaimId == claimId)
            .Select(claim => new ClaimDto(
                claim.ClaimId,
                claim.DateSinistre,
                claim.TypeSinistre,
                claim.Description,
                claim.MontantEstime,
                claim.MontantIndemnisation,
                claim.Statut))
            .SingleOrDefaultAsync(cancellationToken);

    public Task<ClaimContextDto?> GetClaimContextAsync(
        string claimId,
        CancellationToken cancellationToken = default) =>
        _dbContext.Sinistres
            .AsNoTracking()
            .Where(claim => claim.ClaimId == claimId)
            .Select(claim => new ClaimContextDto(
                new ClaimDto(
                    claim.ClaimId,
                    claim.DateSinistre,
                    claim.TypeSinistre,
                    claim.Description,
                    claim.MontantEstime,
                    claim.MontantIndemnisation,
                    claim.Statut),
                new CustomerDto(
                    claim.Client.ClientId,
                    claim.Client.Prenom,
                    claim.Client.Nom,
                    claim.Client.Gouvernorat),
                new ContractDto(
                    claim.Contract.ContractId,
                    claim.Contract.TypeCouverture,
                    claim.Contract.DateDebut,
                    claim.Contract.DateFin),
                new VehicleDto(
                    claim.Vehicle.VehicleId,
                    claim.Vehicle.TypeVehicule,
                    claim.Vehicle.Marque,
                    claim.Vehicle.Modele,
                    claim.Vehicle.Immatriculation)))
            .SingleOrDefaultAsync(cancellationToken);
}
