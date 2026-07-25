using System.Net.Http.Json;
using System.Text.Json;
using AstreeClaims.Api.DTOs.Generation;
using AstreeClaims.Api.Exceptions;

namespace AstreeClaims.Api.Services.Generation;

public sealed class AiGenerationClient : IAiGenerationClient
{
    private readonly HttpClient _httpClient;

    public AiGenerationClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<AiGenerationResponseDto> GenerateAsync(
        AiGenerationRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await _httpClient.PostAsJsonAsync(
                "/api/v1/generate",
                request,
                cancellationToken);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<AiGenerationResponseDto>(
                    cancellationToken: cancellationToken)
                ?? throw new InvalidDataException("Réponse vide du service IA.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (
            exception is HttpRequestException or TaskCanceledException or InvalidDataException or JsonException)
        {
            throw new AiServiceUnavailableException(exception);
        }
    }
}
