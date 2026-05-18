using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Trading313.Api.Data;
using Trading313.Api.Domain.Entities;

namespace Trading313.Tests;

/// <summary>
/// Creates a fresh in-memory SQLite database per test. SQLite is lenient about
/// MySQL-specific column types (decimal(18,4), datetime(6)) — it accepts the
/// strings and stores values as TEXT/NUMERIC. Good enough for unit tests that
/// exercise business logic, not the persistence layer's nuances.
/// </summary>
public sealed class TestDb : IDisposable
{
    public SqliteConnection Connection { get; }
    public AppDbContext Context { get; }

    public TestDb()
    {
        Connection = new SqliteConnection("DataSource=:memory:");
        Connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(Connection)
            .EnableSensitiveDataLogging()
            .Options;

        Context = new AppDbContext(options);
        Context.Database.EnsureCreated();
    }

    public ApplicationUser SeedUser(string id = "user-1", decimal cashBalance = 10_000m)
    {
        var user = new ApplicationUser
        {
            Id = id,
            UserName = id + "@example.com",
            NormalizedUserName = (id + "@example.com").ToUpperInvariant(),
            Email = id + "@example.com",
            NormalizedEmail = (id + "@example.com").ToUpperInvariant(),
            DisplayName = id,
            CashBalance = cashBalance,
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddYears(-1),
            EmailConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
        };
        Context.Users.Add(user);
        Context.SaveChanges();
        return user;
    }

    public void SeedHistoricalPrices(string symbol, DateOnly start, int days, decimal startPrice, decimal endPrice)
    {
        for (var i = 0; i < days; i++)
        {
            var date = start.AddDays(i);
            // Linear interpolation; good enough for tests that need a price-by-date function.
            var t = days == 1 ? 0 : (decimal)i / (days - 1);
            var close = startPrice + (endPrice - startPrice) * t;
            Context.HistoricalPrices.Add(new HistoricalPrice
            {
                Symbol = symbol,
                Date = date,
                Open = close,
                High = close,
                Low = close,
                Close = close,
                Volume = 1_000_000,
            });
        }
        Context.SaveChanges();
    }

    public void Dispose()
    {
        Context.Dispose();
        Connection.Dispose();
    }
}
