using System.Text.Json;
using AstreeClaims.Api.Data;
using AstreeClaims.Api.Services.Claims;
using AstreeClaims.Api.Services.Import;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
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
        return Results.Problem(
            title: "AI service unavailable",
            detail: exception.Message,
            statusCode: 502);
    }
});

app.Run();
