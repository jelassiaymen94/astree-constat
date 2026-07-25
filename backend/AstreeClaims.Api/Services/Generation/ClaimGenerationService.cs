using AstreeClaims.Api.Data;
using AstreeClaims.Api.DTOs.Generation;
using AstreeClaims.Api.Exceptions;
using AstreeClaims.Api.Models;
using AstreeClaims.Api.Services.Claims;
using Microsoft.EntityFrameworkCore;

namespace AstreeClaims.Api.Services.Generation;

public sealed class ClaimGenerationService : IClaimGenerationService
{
    private readonly AstreeClaimsDbContext _dbContext;
    private readonly IClaimsService _claimsService;
    private readonly IAiGenerationClient _aiClient;

    public ClaimGenerationService(
        AstreeClaimsDbContext dbContext,
        IClaimsService claimsService,
        IAiGenerationClient aiClient)
    {
        _dbContext = dbContext;
        _claimsService = claimsService;
        _aiClient = aiClient;
    }

    public async Task<GenerationDto> GenerateAsync(
        string claimId,
        GenerateClaimRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var normalizedClaimId = claimId.Trim();
        var context = await _claimsService.GetClaimContextAsync(
            normalizedClaimId,
            cancellationToken)
            ?? throw new ClaimNotFoundException(normalizedClaimId);

        var generationType = request.GenerationType.Trim().ToLowerInvariant();
        var instruction = string.IsNullOrWhiteSpace(request.UserInstruction)
            ? null
            : request.UserInstruction.Trim();
        var log = new GenerationLog
        {
            GenerationId = Guid.NewGuid(),
            ClaimId = normalizedClaimId,
            GenerationType = generationType,
            UserInstruction = instruction,
            PromptVersion = "1.0",
            CreatedAt = DateTime.UtcNow,
            Success = false
        };

        try
        {
            var generated = await _aiClient.GenerateAsync(
                new AiGenerationRequestDto(generationType, instruction, context),
                cancellationToken);

            log.GeneratedContent = generated.Content;
            log.ModelName = generated.ModelName;
            log.PromptVersion = generated.PromptVersion;
            log.DurationMs = generated.DurationMs;
            log.Success = true;
            _dbContext.GenerationLogs.Add(log);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return Map(log);
        }
        catch (AiServiceUnavailableException exception)
        {
            log.ErrorMessage = exception.Message;
            _dbContext.GenerationLogs.Add(log);
            await _dbContext.SaveChangesAsync(cancellationToken);
            throw;
        }
    }

    public async Task<IReadOnlyList<GenerationDto>> GetHistoryAsync(
        string claimId,
        CancellationToken cancellationToken = default)
    {
        var normalizedClaimId = claimId.Trim();
        var exists = await _dbContext.Sinistres
            .AsNoTracking()
            .AnyAsync(claim => claim.ClaimId == normalizedClaimId, cancellationToken);
        if (!exists)
        {
            throw new ClaimNotFoundException(normalizedClaimId);
        }

        return await _dbContext.GenerationLogs
            .AsNoTracking()
            .Where(log => log.ClaimId == normalizedClaimId)
            .OrderByDescending(log => log.CreatedAt)
            .Select(log => new GenerationDto(
                log.GenerationId,
                log.ClaimId,
                log.GenerationType,
                log.UserInstruction,
                log.GeneratedContent,
                log.ModelName,
                log.PromptVersion,
                log.Success,
                log.ErrorMessage,
                log.CreatedAt,
                log.DurationMs,
                true))
            .ToListAsync(cancellationToken);
    }

    private static GenerationDto Map(GenerationLog log) => new(
        log.GenerationId,
        log.ClaimId,
        log.GenerationType,
        log.UserInstruction,
        log.GeneratedContent,
        log.ModelName,
        log.PromptVersion,
        log.Success,
        log.ErrorMessage,
        log.CreatedAt,
        log.DurationMs,
        true);
}
