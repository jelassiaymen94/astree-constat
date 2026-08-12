using AstreeClaims.Api.DTOs.Claims;
using AstreeClaims.Api.Services.Claims;
using AstreeClaims.Api.Tests.Fixtures;

namespace AstreeClaims.Api.Tests.Services;

public sealed class ClaimsSearchByCustomerNameTests
{
    [Fact]
    public async Task GetClaims_searches_by_customer_first_name_and_last_name()
    {
        await using var context = await SqliteTestContext.CreateAsync();
        var service = new ClaimsService(context.Db);

        var result = await service.GetClaimsAsync(
            new ClaimListQueryDto { Search = "Amine Ben Salah" });

        Assert.Equal(2, result.Total);
        Assert.Equal(["CLM-1001", "CLM-1002"], result.Items.Select(item => item.ClaimId));
    }

    [Fact]
    public async Task GetClaims_searches_by_customer_last_name_and_first_name()
    {
        await using var context = await SqliteTestContext.CreateAsync();
        var service = new ClaimsService(context.Db);

        var result = await service.GetClaimsAsync(
            new ClaimListQueryDto { Search = "Trabelsi Nour" });

        Assert.Equal(2, result.Total);
        Assert.Equal(["CLM-2001", "CLM-3001"], result.Items.Select(item => item.ClaimId));
    }
}
