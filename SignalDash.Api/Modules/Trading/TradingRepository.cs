using System.Data;
using Dapper;

namespace SignalDash.Api.Modules.Trading;

/// <summary>Dapper-backed implementation over the trn_* trading tables.</summary>
public sealed class TradingRepository(IDbConnection db) : ITradingRepository
{
    public Task<IReadOnlyList<HealthRow>> GetHealthAsync(CancellationToken ct = default)
        => QueryAsync<HealthRow>(db, """
            SELECT DISTINCT ON (market) market,
                   equity, ts,
                   EXTRACT(EPOCH FROM (now() - ts))::int AS "AgeSeconds"
            FROM trn_equity_snapshot ORDER BY market, ts DESC
            """, ct);

    public Task<IReadOnlyList<EquityRow>> GetEquityAsync(string? market, CancellationToken ct = default)
        => QueryAsync<EquityRow>(db, """
            SELECT market, equity, ts FROM trn_equity_snapshot
            WHERE (@market IS NULL OR market = @market)
            ORDER BY ts
            """, ct, new { market });

    public Task<IReadOnlyList<SignalRow>> GetSignalsAsync(string? market, string? pair, int limit, CancellationToken ct = default)
        => QueryAsync<SignalRow>(db, """
            SELECT market, pair, side, signal_ts, entry, sl, tp, reason, created_at
            FROM trn_signal
            WHERE (@market IS NULL OR market = @market)
              AND (@pair IS NULL OR pair = @pair)
            ORDER BY signal_ts DESC LIMIT @limit
            """, ct, new { market, pair, limit });

    public Task<IReadOnlyList<TradeRow>> GetTradesAsync(string? market, int limit, CancellationToken ct = default)
        => QueryAsync<TradeRow>(db, """
            SELECT t.pair, t.side, t.entry, t.exit, t.pnl, t.reason,
                   t.opened_at, t.closed_at, s.market
            FROM trn_trade t
            LEFT JOIN trn_signal s ON s.pair = t.pair AND s.signal_ts = t.opened_at
            WHERE (@market IS NULL OR s.market = @market)
            ORDER BY t.closed_at DESC LIMIT @limit
            """, ct, new { market, limit });

    public Task<IReadOnlyList<StatsRow>> GetStatsAsync(CancellationToken ct = default)
        => QueryAsync<StatsRow>(db, """
            SELECT COALESCE(s.market, 'unknown') AS market, count(*) AS trades,
                   count(*) FILTER (WHERE t.pnl > 0) AS wins,
                   COALESCE(SUM(t.pnl), 0) AS net_pnl,
                   CASE WHEN count(*) > 0 THEN count(*) FILTER (WHERE t.pnl > 0)::float / count(*) END AS win_rate,
                   CASE WHEN SUM(t.pnl) FILTER (WHERE t.pnl < 0) = 0 THEN NULL
                        ELSE ABS(SUM(t.pnl) FILTER (WHERE t.pnl > 0) / SUM(t.pnl) FILTER (WHERE t.pnl < 0)) END AS profit_factor
            FROM trn_trade t LEFT JOIN trn_signal s ON s.pair = t.pair AND s.signal_ts = t.opened_at
            GROUP BY COALESCE(s.market, 'unknown')
            """, ct);

    public Task<IReadOnlyList<PositionRow>> GetPositionsAsync(string? market, CancellationToken ct = default)
        => QueryAsync<PositionRow>(db, """
            SELECT DISTINCT ON (market, pair) market, pair, side, entry, sl, tp,
                   units, opened_at, snapshot_ts
            FROM trn_position
            WHERE (@market IS NULL OR market = @market)
            ORDER BY market, pair, snapshot_ts DESC
            """, ct, new { market });

    private static async Task<IReadOnlyList<T>> QueryAsync<T>(
        IDbConnection db, string sql, CancellationToken ct, object? p = null)
        => (await db.QueryAsync<T>(new CommandDefinition(sql, p, cancellationToken: ct))).ToList();
}
