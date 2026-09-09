using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NevDBClass.Data;

namespace MobileNevApps.Tests.Helpers;

internal sealed class TestDbContextFactory : IDbContextFactory<SqLiteDbContext>, IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<SqLiteDbContext> _options;

    public TestDbContextFactory()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<SqLiteDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var db = CreateDbContext();
        db.Database.EnsureCreated();
    }

    public SqLiteDbContext CreateDbContext() => new(_options);

    public void Dispose()
    {
        _connection.Dispose();
    }
}
