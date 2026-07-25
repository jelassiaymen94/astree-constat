using System.Diagnostics;
using System.Text.Json;
using AstreeClaims.Api.Data;
using AstreeClaims.Api.DTOs.Common;
using AstreeClaims.Api.ErrorHandling;
using AstreeClaims.Api.Exceptions;
using AstreeClaims.Api.Services.Claims;
using AstreeClaims.Api.Services.Generation;
using AstreeClaims.Api.Services.Import;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState.Where(entry => entry.Value?.Errors.Count > 0).ToDictionary(entry => entry.Key, entry => entry.Value!.Errors.Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage) ? "Valeur invalide." : error.ErrorMessage).ToArray());
        var traceId = Activity.Current?.Id ?? context.HttpContext.TraceIdentifier;
        return new BadRequestObjectResult(new ApiErrorDto("INVALID_REQUEST", "Un ou plusieurs paramètres sont invalides.", traceId, errors));
    };
});
builder.Services.AddDbContext<AstreeClaimsDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IDataImportService, DataImportService>();
builder.Services.AddScoped<IClaimsService, ClaimsService>();
builder.Services.AddScoped<IClaimGenerationService, ClaimGenerationService>();
var aiServiceBaseUrl = builder.Configuration["AiService:BaseUrl"] ?? "http://localhost:8000";
var aiServiceTimeoutSeconds = builder.Configuration.GetValue<int?>("AiService:TimeoutSeconds") ?? 30;
builder.Services.AddHttpClient("AiService", client => { client.BaseAddress = new Uri(aiServiceBaseUrl); client.Timeout = TimeSpan.FromSeconds(aiServiceTimeoutSeconds); });
builder.Services.AddHttpClient<IAiGenerationClient, AiGenerationClient>(client => { client.BaseAddress = new Uri(aiServiceBaseUrl); client.Timeout = TimeSpan.FromSeconds(aiServiceTimeoutSeconds); });
var app = builder.Build();
if (args.Contains("--import-data", StringComparer.OrdinalIgnoreCase))
{
    var index = Array.FindIndex(args, argument => string.Equals(argument, "--import-dir", StringComparison.OrdinalIgnoreCase));
    var directory = index >= 0 && index + 1 < args.Length ? args[index + 1] : Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "../../data/processed"));
    using var scope = app.Services.CreateScope();
    var result = await scope.ServiceProvider.GetRequiredService<IDataImportService>().ImportAsync(directory);
    Console.WriteLine(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
    return;
}
app.UseExceptionHandler();
if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }
if (!app.Environment.IsEnvironment("Testing")) app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/api/health/database", async (AstreeClaimsDbContext db) => Results.Ok(new { database = "AstreeClaimsDb", connected = await db.Database.CanConnectAsync() }));
app.MapGet("/api/health/ai", async (IHttpClientFactory factory) =>
{
    try
    {
        var response = await factory.CreateClient("AiService").GetAsync("/health");
        return Results.Ok(new { service = "astree-ai-service", connected = response.IsSuccessStatusCode, statusCode = (int)response.StatusCode, response = await response.Content.ReadAsStringAsync() });
    }
    catch (Exception exception) { throw new AiServiceUnavailableException(exception); }
});
app.Run();
public partial class Program { }
