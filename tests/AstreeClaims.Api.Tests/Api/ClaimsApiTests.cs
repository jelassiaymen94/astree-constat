using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AstreeClaims.Api.DTOs.Claims;
using AstreeClaims.Api.DTOs.Common;
using AstreeClaims.Api.Tests.Fixtures;

namespace AstreeClaims.Api.Tests.Api;

public sealed class ClaimsApiTests : IClassFixture<ClaimsApiFactory>
{
    private readonly HttpClient _client;

    public ClaimsApiTests(ClaimsApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task List_returns_http_200_and_paged_payload()
    {
        var response = await _client.GetAsync("/api/claims?page=1&pageSize=2");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<PagedResultDto<ClaimDto>>();
        Assert.NotNull(body);
        Assert.Equal(4, body.Total);
        Assert.Equal(2, body.Items.Count);
    }

    [Theory]
    [InlineData("/api/claims?page=0")]
    [InlineData("/api/claims?pageSize=101")]
    public async Task Invalid_pagination_returns_standard_http_400(string url)
    {
        var response = await _client.GetAsync(url);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorDto>();
        Assert.NotNull(error);
        Assert.Equal("INVALID_REQUEST", error.Code);
        Assert.False(string.IsNullOrWhiteSpace(error.TraceId));
        Assert.NotNull(error.Errors);
    }

    [Fact]
    public async Task Unknown_claim_returns_404_claim_not_found_with_trace_id()
    {
        var response = await _client.GetAsync("/api/claims/CLM-UNKNOWN");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorDto>();
        Assert.NotNull(error);
        Assert.Equal("CLAIM_NOT_FOUND", error.Code);
        Assert.Contains("CLM-UNKNOWN", error.Message);
        Assert.False(string.IsNullOrWhiteSpace(error.TraceId));
        Assert.Null(error.Errors);
    }

    [Fact]
    public async Task Public_error_payload_does_not_expose_a_stack_trace()
    {
        var response = await _client.GetAsync("/api/claims/CLM-UNKNOWN");
        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);

        Assert.False(document.RootElement.TryGetProperty("stackTrace", out _));
        Assert.False(document.RootElement.TryGetProperty("exception", out _));
        Assert.True(document.RootElement.TryGetProperty("traceId", out var traceId));
        Assert.False(string.IsNullOrWhiteSpace(traceId.GetString()));
    }
}
