using AstreeClaims.Api.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AstreeClaims.Api.Tests.Fixtures;

internal sealed class SqliteTestContext : IAsyncDisposable
{
    private readonly SqliteConnection _connection;

    private SqliteTestContext(SqliteConnection connection, AstreeClaimsDbContext db)
    {
        _connection = connection;
        Db = db;
    }

    public AstreeClaimsDbContext Db { get; }

    public static async Task<SqliteTestContext> CreateAsync(bool seed = true)
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AstreeClaimsDbContext>()
            .UseSqlite(connection)
            .Options;
        var db = new AstreeClaimsDbContext(options);
        await db.Database.EnsureCreatedAsync();
        if (seed)
        {
            TestData.Seed(db);
        }

        return new SqliteTestContext(connection, db);
    }

    public async ValueTask DisposeAsync()
    {
        await Db.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
