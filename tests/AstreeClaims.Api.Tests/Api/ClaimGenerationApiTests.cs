using System.Net;
using System.Net.Http.Json;
using AstreeClaims.Api.DTOs.Common;
using AstreeClaims.Api.DTOs.Generation;
using AstreeClaims.Api.Tests.Fixtures;

namespace AstreeClaims.Api.Tests.Api;

public sealed class ClaimGenerationApiTests : IClassFixture<ClaimsApiFactory>
{
    private readonly HttpClient _client;
    public ClaimGenerationApiTests(ClaimsApiFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Generate_returns_a_draft_requiring_human_validation()
    {
        var response = await _client.PostAsJsonAsync("/api/claims/CLM-1001/generate", new { generationType = "summary", userInstruction = "Rester concis" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<GenerationDto>();
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.True(result.RequiresHumanValidation);
        Assert.Equal("fake-test-model", result.ModelName);
    }

    [Fact]
    public async Task Generated_content_is_available_in_history()
    {
        var response = await _client.PostAsJsonAsync("/api/claims/CLM-2001/generate", new { generationType = "letter" });
        response.EnsureSuccessStatusCode();
        var generated = await response.Content.ReadFromJsonAsync<GenerationDto>();
        var history = await _client.GetFromJsonAsync<List<GenerationDto>>("/api/claims/CLM-2001/generations");
        Assert.NotNull(generated);
        Assert.NotNull(history);
        Assert.Contains(history, item => item.GenerationId == generated.GenerationId && item.Success);
    }

    [Fact]
    public async Task Invalid_generation_type_returns_http_400()
    {
        var response = await _client.PostAsJsonAsync("/api/claims/CLM-1001/generate", new { generationType = "automatic-decision" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("INVALID_REQUEST", (await response.Content.ReadFromJsonAsync<ApiErrorDto>())!.Code);
    }

    [Fact]
    public async Task Unknown_claim_returns_http_404()
    {
        var response = await _client.PostAsJsonAsync("/api/claims/CLM-UNKNOWN/generate", new { generationType = "summary" });
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Ai_outage_returns_http_502_and_records_failed_attempt()
    {
        var response = await _client.PostAsJsonAsync("/api/claims/CLM-1002/generate", new { generationType = "response", userInstruction = "simulate-unavailable" });
        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        var history = await _client.GetFromJsonAsync<List<GenerationDto>>("/api/claims/CLM-1002/generations");
        Assert.NotNull(history);
        Assert.Contains(history, item => !item.Success && item.GenerationType == "response");
    }
}
