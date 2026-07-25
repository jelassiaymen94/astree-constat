using AstreeClaims.Api.Data;
using AstreeClaims.Api.Services.Generation;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AstreeClaims.Api.Tests.Fixtures;

public sealed class ClaimsApiFactory : WebApplicationFactory<Program>
{
    private SqliteConnection? _connection;
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AstreeClaimsDbContext>>();
            services.RemoveAll<AstreeClaimsDbContext>();
            services.RemoveAll<IAiGenerationClient>();
            _connection = new SqliteConnection("Data Source=:memory:");
            _connection.Open();
            services.AddDbContext<AstreeClaimsDbContext>(options => options.UseSqlite(_connection));
            services.AddSingleton<IAiGenerationClient, FakeAiGenerationClient>();
            using var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AstreeClaimsDbContext>();
            db.Database.EnsureCreated();
            TestData.Seed(db);
        });
    }
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) _connection?.Dispose();
    }
}
