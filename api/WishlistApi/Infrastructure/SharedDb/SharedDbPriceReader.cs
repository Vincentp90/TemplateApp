using System.Data;
using Application.Contracts;
using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Infrastructure.SharedDb;

/// <summary>
/// Dapper-based reader for SteamTracker's database.
/// Reads prices from the games table and alert rules from the alert_rules table.
/// Uses snake_case column names with PascalCase aliases for Dapper materialization.
/// </summary>
public class SharedDbPriceReader : ISharedDbPriceReader
{
    private readonly string _connectionString;

    public SharedDbPriceReader(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("SteamTrackerConnection")!;
    }

    public async Task<Dictionary<int, GamePrice>> GetPricesAsync(IEnumerable<int> appIds)
    {
        if (appIds == null)
            return new Dictionary<int, GamePrice>();

        var ids = appIds.ToList();
        if (!ids.Any())
            return new Dictionary<int, GamePrice>();

        try
        {
            using var connection = new NpgsqlConnection(_connectionString);

            const string sql = @"
                SELECT ""app_id"" AS ""AppId"", ""current_price"" AS ""CurrentPrice"", ""last_checked_at"" AS ""LastCheckedAt"", ""is_unavailable"" AS ""IsUnavailable""
                FROM ""games""
                WHERE ""app_id"" = ANY(@Ids)";

            var rows = await connection.QueryAsync<GamePriceRow>(sql, new { Ids = ids });

            return rows
                .Where(r => r.AppId > 0)
                .Select(ParseGamePrice)
                .ToDictionary(r => r.AppId, r => r.Price);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SharedDbPriceReader] Failed to read prices from SteamTracker DB: {ex.Message}");
            return new Dictionary<int, GamePrice>();
        }
    }

    public async Task<Dictionary<int, AlertRuleInfo>> GetAlertRulesAsync(string userId)
    {
        try
        {
            using var connection = new NpgsqlConnection(_connectionString);

            // TriggerBelowPrice is stored as "Amount|Currency" (combined column per AlertRuleConfig)
            const string sql = @"
                SELECT ""alert_rule_id"" AS ""AlertRuleId"", ""app_id"" AS ""AppId"", ""trigger_below_price"" AS ""TriggerBelowPrice""
                FROM ""alert_rules""
                WHERE ""user_id"" = @UserId AND ""is_active"" = true";

            var rows = await connection.QueryAsync<AlertRuleRow>(sql, new { UserId = userId });

            return rows
                .Where(r => r.AppId > 0)
                .Select(ParseAlertRule)
                .Where(r => r.HasValue)
                .ToDictionary(
                    r => r!.Value.AppId,
                    r => r!.Value.Info);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SharedDbPriceReader] Failed to read alert rules from SteamTracker DB: {ex.Message}");
            return new Dictionary<int, AlertRuleInfo>();
        }
    }

    private static (decimal Amount, string Currency) ParseMoneyString(string value)
    {
        var parts = value.Split('|');
        return parts.Length == 2
            ? (decimal.Parse(parts[0]), parts[1])
            : (0, string.Empty);
    }

    private static (int AppId, GamePrice Price) ParseGamePrice(GamePriceRow row)
    {
        decimal? amount = null;
        string? currency = null;

        if (!string.IsNullOrWhiteSpace(row.CurrentPrice))
        {
            var money = ParseMoneyString(row.CurrentPrice);
            if (money.Amount >= 0 && !string.IsNullOrWhiteSpace(money.Currency))
            {
                amount = money.Amount;
                currency = money.Currency;
            }
        }

        return (row.AppId, new GamePrice(amount, currency!, ToDateTimeOffset(row.LastCheckedAt), row.IsUnavailable));
    }

    private static (int AppId, AlertRuleInfo Info)? ParseAlertRule(AlertRuleRow row)
    {
        if (row.AppId <= 0) return null;
        var money = ParseMoneyString(row.TriggerBelowPrice);
        if (money.Amount <= 0) return null;
        return (row.AppId, new AlertRuleInfo(row.AlertRuleId, money.Amount, money.Currency));
    }

    private record GamePriceRow(int AppId, string? CurrentPrice, DateTime? LastCheckedAt, bool IsUnavailable);

    private static DateTimeOffset? ToDateTimeOffset(DateTime? dt)
    {
        if (!dt.HasValue) return null;
        return new DateTimeOffset(dt.Value.ToUniversalTime(), TimeSpan.Zero);
    }
    private record AlertRuleRow(Guid AlertRuleId, int AppId, string TriggerBelowPrice);
}
