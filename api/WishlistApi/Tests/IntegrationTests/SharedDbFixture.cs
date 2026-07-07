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
    /// Seeds SteamTracker's snake_case tables with test data using raw SQL.
    /// Tables: tracked_games, games, alert_rules (matching EF Core snake_case naming convention).
    /// </summary>
    public async Task SeedSteamTrackerAsync(string connectionString)
    {
        // Ensure the steamtracker database exists before seeding tables
        await EnsureDatabaseCreatedAsync(connectionString);

        using var connection = new Npgsql.NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        const string sql = @"
            -- Drop and recreate tables to ensure schema matches EF Core
            DROP TABLE IF EXISTS ""price_snapshots"";
            DROP TABLE IF EXISTS ""alert_rules"";
            DROP TABLE IF EXISTS ""games"";
            DROP TABLE IF EXISTS ""tracked_games"";

            -- Create tracked_games table (snake_case to match EF Core naming convention)
            CREATE TABLE ""tracked_games"" (
                ""app_id"" INT PRIMARY KEY,
                ""is_active"" BOOLEAN NOT NULL,
                ""tracked_since"" TIMESTAMPTZ NOT NULL
            );

            -- Create games table (snake_case to match EF Core naming convention)
            CREATE TABLE ""games"" (
                ""app_id"" INT PRIMARY KEY,
                ""name"" VARCHAR(256) NOT NULL,
                ""current_price"" TEXT,
                ""last_checked_at"" TIMESTAMPTZ
            );

            -- Create alert_rules table (snake_case to match EF Core naming convention)
            CREATE TABLE ""alert_rules"" (
                ""alert_rule_id"" UUID PRIMARY KEY,
                ""user_id"" VARCHAR(128) NOT NULL,
                ""app_id"" INT NOT NULL,
                ""trigger_below_price"" VARCHAR(20) NOT NULL,
                ""is_active"" BOOLEAN NOT NULL DEFAULT true,
                ""last_triggered_at"" TIMESTAMPTZ
            );

            -- Insert test tracked games
            INSERT INTO ""tracked_games"" (""app_id"", ""is_active"", ""tracked_since"") VALUES
                (42, true, '2025-01-01T00:00:00Z'),
                (100, true, '2025-01-02T00:00:00Z'),
                (200, true, '2025-01-03T00:00:00Z'),
                (300, false, '2025-01-04T00:00:00Z')
            ON CONFLICT (""app_id"") DO NOTHING;

            -- Insert test games with prices (current_price is the EF Core string column ""Amount|Currency"")
            INSERT INTO ""games"" (""app_id"", ""name"", ""current_price"", ""last_checked_at"") VALUES
                (42, 'Test Game Alpha', '19.99|EUR', '2025-07-01T12:00:00Z'),
                (100, 'Test Game Beta', '29.99|EUR', '2025-07-01T12:00:00Z'),
                (200, 'Free To Play Game', NULL, NULL)
            ON CONFLICT (""app_id"") DO NOTHING;

            -- Insert test alert rules
            INSERT INTO ""alert_rules"" (""alert_rule_id"", ""user_id"", ""app_id"", ""trigger_below_price"", ""is_active"", ""last_triggered_at"") VALUES
                ('a0000000-0000-0000-0000-000000000001'::uuid, 'user-1', 42, '15.00|EUR', true, NULL),
                ('a0000000-0000-0000-0000-000000000002'::uuid, 'user-1', 100, '25.00|EUR', true, NULL),
                ('a0000000-0000-0000-0000-000000000003'::uuid, 'user-1', 200, '5.00|EUR', false, NULL),
                ('a0000000-0000-0000-0000-000000000004'::uuid, 'user-2', 42, '10.00|EUR', true, '2025-06-01T10:00:00Z')
            ON CONFLICT (""alert_rule_id"") DO NOTHING;
        ";

        using var command = new Npgsql.NpgsqlCommand(sql, connection);
        try
        {
            await command.ExecuteNonQueryAsync();
        }
        catch (Npgsql.PostgresException ex) when (ex.SqlState == "23505" || ex.SqlState == "42710")
        {
            // Data already seeded — ignore unique violations (23505) or duplicate tables (42710)
        }
    }
}
