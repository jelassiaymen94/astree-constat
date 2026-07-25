using AstreeClaims.Api.DTOs.Claims;
using AstreeClaims.Api.Services.Claims;
using AstreeClaims.Api.Tests.Fixtures;

namespace AstreeClaims.Api.Tests.Services;

public sealed class ClaimsServiceTests
{
    [Fact]
    public async Task GetClaims_returns_first_page_in_stable_order()
    {
        await using var context = await SqliteTestContext.CreateAsync();
        var service = new ClaimsService(context.Db);

        var result = await service.GetClaimsAsync(new ClaimListQueryDto
        {
            Page = 1,
            PageSize = 2
        });

        Assert.Equal(4, result.Total);
        Assert.Equal(["CLM-1001", "CLM-1002"], result.Items.Select(x => x.ClaimId));
    }

    [Fact]
    public async Task GetClaims_returns_a_different_second_page()
    {
        await using var context = await SqliteTestContext.CreateAsync();
        var service = new ClaimsService(context.Db);

        var result = await service.GetClaimsAsync(new ClaimListQueryDto
        {
            Page = 2,
            PageSize = 2
        });

        Assert.Equal(["CLM-2001", "CLM-3001"], result.Items.Select(x => x.ClaimId));
    }

    [Fact]
    public async Task GetClaims_filters_by_status()
    {
        await using var context = await SqliteTestContext.CreateAsync();
        var service = new ClaimsService(context.Db);

        var result = await service.GetClaimsAsync(new ClaimListQueryDto { Status = " Ouvert " });

        Assert.Equal(2, result.Total);
        Assert.All(result.Items, item => Assert.Equal("Ouvert", item.Status));
    }

    [Fact]
    public async Task GetClaims_filters_by_type()
    {
        await using var context = await SqliteTestContext.CreateAsync();
        var service = new ClaimsService(context.Db);

        var result = await service.GetClaimsAsync(new ClaimListQueryDto { Type = "Accident" });

        Assert.Equal(2, result.Total);
        Assert.All(result.Items, item => Assert.Equal("Accident", item.Type));
    }

    [Fact]
    public async Task GetClaims_searches_by_identifier()
    {
        await using var context = await SqliteTestContext.CreateAsync();
        var service = new ClaimsService(context.Db);

        var result = await service.GetClaimsAsync(new ClaimListQueryDto { Search = "2001" });

        var item = Assert.Single(result.Items);
        Assert.Equal("CLM-2001", item.ClaimId);
    }

    [Fact]
    public async Task GetClaim_returns_existing_claim()
    {
        await using var context = await SqliteTestContext.CreateAsync();
        var service = new ClaimsService(context.Db);

        var claim = await service.GetClaimAsync("CLM-1001");

        Assert.NotNull(claim);
        Assert.Equal("Accident", claim.Type);
    }

    [Fact]
    public async Task GetClaim_returns_null_for_unknown_claim()
    {
        await using var context = await SqliteTestContext.CreateAsync();
        var service = new ClaimsService(context.Db);

        var claim = await service.GetClaimAsync("CLM-UNKNOWN");

        Assert.Null(claim);
    }

    [Fact]
    public async Task GetClaimContext_returns_coherent_relations()
    {
        await using var context = await SqliteTestContext.CreateAsync();
        var service = new ClaimsService(context.Db);

        var result = await service.GetClaimContextAsync("CLM-1001");

        Assert.NotNull(result);
        Assert.Equal("CLI-001", result.Customer.ClientId);
        Assert.Equal("CTR-001", result.Contract.ContractId);
        Assert.Equal("VEH-001", result.Vehicle.VehicleId);
        Assert.Equal("CLM-1001", result.Claim.ClaimId);
        Assert.InRange(result.Claim.Date, result.Contract.StartDate, result.Contract.EndDate);
    }
}
