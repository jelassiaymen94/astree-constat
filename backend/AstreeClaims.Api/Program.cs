using System.Diagnostics;
using System.Text.Json;
using AstreeClaims.Api.Data;
using AstreeClaims.Api.DTOs.Common;
using AstreeClaims.Api.ErrorHandling;
using AstreeClaims.Api.Exceptions;
using AstreeClaims.Api.Services.Claims;
using AstreeClaims.Api.Services.Import;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(entry => entry.Value?.Errors.Count > 0)
            .ToDictionary(
                entry => entry.Key,
                entry => entry.Value!.Errors
                    .Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage)
                        ? "Valeur invalide."
                        : error.ErrorMessage)
                    .ToArray());

        var traceId = Activity.Current?.Id
            ?? context.HttpContext.TraceIdentifier;

        return new BadRequestObjectResult(new ApiErrorDto(
            "INVALID_REQUEST",
            "Un ou plusieurs paramètres sont invalides.",
            traceId,
            errors));
    };
});

builder.Services.AddDbContext<AstreeClaimsDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IDataImportService, DataImportService>();
builder.Services.AddScoped<IClaimsService, ClaimsService>();

var aiServiceBaseUrl =
    builder.Configuration["AiService:BaseUrl"]
    ?? "http://localhost:8000";

builder.Services.AddHttpClient("AiService", client =>
{
    client.BaseAddress = new Uri(aiServiceBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(10);
});

var app = builder.Build();

// Import manuel et idempotent des fichiers préparés.
if (args.Contains("--import-data", StringComparer.OrdinalIgnoreCase))
{
    var importDirectoryArgumentIndex = Array.FindIndex(
        args,
        argument => string.Equals(
            argument,
            "--import-dir",
            StringComparison.OrdinalIgnoreCase));

    var importDirectory =
        importDirectoryArgumentIndex >= 0 &&
        importDirectoryArgumentIndex + 1 < args.Length
            ? args[importDirectoryArgumentIndex + 1]
            : Path.GetFullPath(
                Path.Combine(builder.Environment.ContentRootPath, "../../data/processed"));

    using var scope = app.Services.CreateScope();
    var importService = scope.ServiceProvider.GetRequiredService<IDataImportService>();
    var importResult = await importService.ImportAsync(importDirectory);

    Console.WriteLine(JsonSerializer.Serialize(
        importResult,
        new JsonSerializerOptions { WriteIndented = true }));

    return;
}

// HTTP pipeline
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsEnvironment("Testing"))
{
    app.UseHttpsRedirection();
}
app.UseAuthorization();

app.MapControllers();

// Vérification SQL Server
app.MapGet("/api/health/database", async (AstreeClaimsDbContext db) =>
{
    var connected = await db.Database.CanConnectAsync();

    return Results.Ok(new
    {
        database = "AstreeClaimsDb",
        connected
    });
});

// Vérification FastAPI
app.MapGet("/api/health/ai", async (
    IHttpClientFactory httpClientFactory) =>
{
    try
    {
        var client = httpClientFactory.CreateClient("AiService");
        var response = await client.GetAsync("/health");
        var content = await response.Content.ReadAsStringAsync();

        return Results.Ok(new
        {
            service = "astree-ai-service",
            connected = response.IsSuccessStatusCode,
            statusCode = (int)response.StatusCode,
            response = content
        });
    }
    catch (Exception exception)
    {
        throw new AiServiceUnavailableException(exception);
    }
});

app.Run();


// Point d’entrée exposé aux tests d’intégration WebApplicationFactory.
public partial class Program { }
