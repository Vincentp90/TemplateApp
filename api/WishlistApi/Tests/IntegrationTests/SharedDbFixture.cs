using Npgsql;
using Testcontainers.PostgreSql;

namespace Tests.IntegrationTests;

/// <summary>
/// Singleton shared Postgres container for SteamTracker integration tests.
/// Both WishlistApi and SteamTracker share the same Postgres instance.
/// </summary>
public sealed class SharedDbFixture : IAsyncLifetime
{
    private static readonly Lazy<SharedDbFixture> _instance = new(() => new());
    public static SharedDbFixture Instance => _instance.Value;

    public PostgreSqlContainer Container { get; }

    private bool _initialized;
    private readonly object _dbLock = new();
    private bool _dbCreated;

    private SharedDbFixture()
    {
        Container = new PostgreSqlBuilder("postgres:18.1")
            .WithDatabase("testdb")
            .WithUsername("user")
            .WithPassword("pass")
            .Build();
    }

    public string WishlistApiConnectionString => Container.GetConnectionString();

    public string SteamTrackerConnectionString => Container.GetConnectionString()
        .Replace("Database=testdb", "Database=steamtracker");

    /// <summary>
    /// Creates the steamtracker database if it doesn't exist.
    /// </summary>
    public async Task EnsureDatabaseCreatedAsync(string steamTrackerConnectionString)
    {
        lock (_dbLock)
        {
            if (_dbCreated)
                return;
        }

        // Connect to the default 'postgres' database to create the new database
        var postgresConn = Container.GetConnectionString().Replace("Database=testdb", "Database=postgres");
        using var connection = new Npgsql.NpgsqlConnection(postgresConn);
        await connection.OpenAsync();

        const string sql = @"
            SELECT 1 FROM pg_database WHERE datname = 'steamtracker';
        ";
        using var command = new Npgsql.NpgsqlCommand(sql, connection);
        var exists = await command.ExecuteScalarAsync();

        if (exists == null)
        {
            const string createSql = "CREATE DATABASE steamtracker";
            using var createCmd = new Npgsql.NpgsqlCommand(createSql, connection);
            try
            {
                await createCmd.ExecuteNonQueryAsync();
            }
            catch (Npgsql.PostgresException ex) when (ex.SqlState == "23505")
            {
                // Another thread created it concurrently — ignore
            }
        }

        lock (_dbLock)
        {
            _dbCreated = true;
        }
    }

    public async Task InitializeAsync()
    {
        if (!_initialized)
        {
            await Container.StartAsync();
            _initialized = true;
        }
    }

    public async Task DisposeAsync()
    {
        if (_initialized)
        {
            await Container.DisposeAsync();
            _initialized = false;
        }
    }

    /// <summary>
    /// Seeds SteamTracker's PascalCase tables with test data using raw SQL.
    /// Tables: tracked_games, games, alert_rules
    /// </summary>
    public async Task SeedSteamTrackerAsync(string connectionString)
    {
        // Ensure the steamtracker database exists before seeding tables
        await EnsureDatabaseCreatedAsync(connectionString);

        using var connection = new Npgsql.NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        const string sql = @"
            -- Create TrackedGames table (PascalCase to match EF Core)
            CREATE TABLE IF NOT EXISTS ""TrackedGames"" (
                ""AppId"" INT PRIMARY KEY,
                ""IsActive"" BOOLEAN NOT NULL,
                ""TrackedSince"" TIMESTAMPTZ NOT NULL
            );

            -- Create Games table (PascalCase to match EF Core)
            CREATE TABLE IF NOT EXISTS ""Games"" (
                ""AppId"" INT PRIMARY KEY,
                ""Name"" VARCHAR(256) NOT NULL,
                ""CurrentPriceAmount"" DECIMAL(10,2),
                ""CurrentPriceCurrency"" VARCHAR(3),
                ""LastCheckedAt"" TIMESTAMPTZ
            );

            -- Create AlertRules table (PascalCase to match EF Core)
            CREATE TABLE IF NOT EXISTS ""AlertRules"" (
                ""AlertRuleId"" UUID PRIMARY KEY,
                ""UserId"" VARCHAR(128) NOT NULL,
                ""AppId"" INT NOT NULL,
                ""TriggerBelowPrice"" VARCHAR(20) NOT NULL,
                ""IsActive"" BOOLEAN NOT NULL DEFAULT true,
                ""LastTriggeredAt"" TIMESTAMPTZ
            );

            -- Insert test tracked games
            INSERT INTO ""TrackedGames"" (""AppId"", ""IsActive"", ""TrackedSince"") VALUES
                (42, true, '2025-01-01T00:00:00Z'),
                (100, true, '2025-01-02T00:00:00Z'),
                (200, true, '2025-01-03T00:00:00Z'),
                (300, false, '2025-01-04T00:00:00Z')
            ON CONFLICT (""AppId"") DO NOTHING;

            -- Insert test games with prices
            INSERT INTO ""Games"" (""AppId"", ""Name"", ""CurrentPriceAmount"", ""CurrentPriceCurrency"", ""LastCheckedAt"") VALUES
                (42, 'Test Game Alpha', 19.99, 'EUR', '2025-07-01T12:00:00Z'),
                (100, 'Test Game Beta', 29.99, 'EUR', '2025-07-01T12:00:00Z'),
                (200, 'Free To Play Game', NULL, NULL, NULL)
            ON CONFLICT (""AppId"") DO NOTHING;

            -- Insert test alert rules
            INSERT INTO ""AlertRules"" (""AlertRuleId"", ""UserId"", ""AppId"", ""TriggerBelowPrice"", ""IsActive"", ""LastTriggeredAt"") VALUES
                ('a0000000-0000-0000-0000-000000000001'::uuid, 'user-1', 42, '15.00|EUR', true, NULL),
                ('a0000000-0000-0000-0000-000000000002'::uuid, 'user-1', 100, '25.00|EUR', true, NULL),
                ('a0000000-0000-0000-0000-000000000003'::uuid, 'user-1', 200, '5.00|EUR', false, NULL),
                ('a0000000-0000-0000-0000-000000000004'::uuid, 'user-2', 42, '10.00|EUR', true, '2025-06-01T10:00:00Z')
            ON CONFLICT (""AlertRuleId"") DO NOTHING;
        ";

        using var command = new Npgsql.NpgsqlCommand(sql, connection);
        try
        {
            await command.ExecuteNonQueryAsync();
        }
        catch (Npgsql.PostgresException ex) when (ex.SqlState == "23505")
        {
            // Data already seeded — ignore unique violations
        }
    }
}
