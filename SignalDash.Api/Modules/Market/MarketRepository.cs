using System.Data;
using Dapper;

namespace SignalDash.Api.Modules.Market;

/// <summary>Dapper-backed implementation over the broker_stock_daily / ob_snapshot tables.</summary>
public sealed class MarketRepository(IDbConnection db) : IMarketRepository
{
    public Task<IReadOnlyList<BrokerRow>> GetBrokersAsync(string? ticker, string? broker, int days, CancellationToken ct = default)
        => QueryAsync<BrokerRow>(db, """
            SELECT ticker, broker_code, investor_type,
                   SUM(CASE WHEN side = 'BUY' THEN net_lots ELSE -net_lots END) AS net_lots,
                   SUM(CASE WHEN side = 'BUY' THEN net_value_idr ELSE -net_value_idr END) AS net_value_idr,
                   ROUND(AVG(avg_price) FILTER (WHERE avg_price IS NOT NULL)::numeric, 2) AS avg_price,
                   MAX(date)::timestamp AS last_date
            FROM broker_stock_daily
            WHERE date >= CURRENT_DATE - @days
              AND (@ticker IS NULL OR ticker = @ticker)
              AND (@broker IS NULL OR broker_code = @broker)
            GROUP BY ticker, broker_code, investor_type
            ORDER BY net_value_idr DESC
            """, ct, new { days, ticker, broker });

    public Task<IReadOnlyList<OrderbookRow>> GetOrderbookAsync(string? ticker, int limit, CancellationToken ct = default)
        => QueryAsync<OrderbookRow>(db, """
            SELECT DISTINCT ON (ticker) ticker, ts, last, imb5, imb10, wall, fnet,
                   total_bid_lot, total_ask_lot
            FROM ob_snapshot
            WHERE (@ticker IS NULL OR ticker = @ticker)
            ORDER BY ticker, ts DESC LIMIT @limit
            """, ct, new { ticker, limit });

    private static async Task<IReadOnlyList<T>> QueryAsync<T>(
        IDbConnection db, string sql, CancellationToken ct, object? p = null)
        => (await db.QueryAsync<T>(new CommandDefinition(sql, p, cancellationToken: ct))).ToList();
}
