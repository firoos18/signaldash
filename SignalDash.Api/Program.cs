using System.Data;
using Dapper;
using Npgsql;

// snake_case DB columns → camelCase record properties
DefaultTypeMap.MatchNamesWithUnderscores = true;

var builder = WebApplication.CreateBuilder(args);
// ponytail: config reload watcher needs inotify (128-instance cap on this VM);
//           static config is fine for a dashboard read API. Re-enable if hot-reload needed.
builder.Configuration.Sources.Clear();
builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);
builder.Configuration.AddEnvironmentVariables();
builder.Services.AddScoped<IDbConnection>(_ => new NpgsqlConnection(
    builder.Configuration.GetConnectionString("SignalDash")
    ?? throw new InvalidOperationException("ConnectionStrings:SignalDash missing")));
builder.Services.AddCors(o => o.AddPolicy("frontend", p =>
    p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }
app.UseCors("frontend");

// ── helpers ──
// ponytail: no repository layer/interface — single read-only API, 4 queries.
//           split into IEquityRepository etc. only when a 2nd consumer appears.
static async Task<IReadOnlyList<T>> Query<T>(IDbConnection db, string sql, object? p = null)
    => (await db.QueryAsync<T>(sql, p)).ToList();

// ── endpoints ──
app.MapGet("/api/health", async (IDbConnection db) =>
{
    // last equity snapshot per market = last bot heartbeat
    var h = await Query<HealthRow>(db, """
        SELECT DISTINCT ON (market) market,
               equity, ts,
               EXTRACT(EPOCH FROM (now() - ts))::int AS "AgeSeconds"
        FROM trn_equity_snapshot ORDER BY market, ts DESC
        """);
    return Results.Ok(new
    {
        status = h.All(x => x.AgeSeconds < 5400) ? "ok" : "stale",
        bots = h.Select(x => new
        {
            x.Market, x.Equity, x.Ts,
            AgeSeconds = x.AgeSeconds,
            Stale = x.AgeSeconds >= 5400,
        })
    });
});

app.MapGet("/api/equity", async (IDbConnection db, string? market = null) =>
{
    var rows = await Query<EquityRow>(db, """
        SELECT market, equity, ts FROM trn_equity_snapshot
        WHERE (@market IS NULL OR market = @market)
        ORDER BY ts
        """, new { market });
    return Results.Ok(rows);
});

app.MapGet("/api/signals", async (IDbConnection db, string? market = null,
    string? pair = null, int limit = 100) =>
{
    limit = Math.Clamp(limit, 1, 1000);
    var rows = await Query<SignalRow>(db, """
        SELECT market, pair, side, signal_ts, entry, sl, tp, reason, created_at
        FROM trn_signal
        WHERE (@market IS NULL OR market = @market)
          AND (@pair IS NULL OR pair = @pair)
        ORDER BY signal_ts DESC LIMIT @limit
        """, new { market, pair, limit });
    return Results.Ok(rows);
});

app.MapGet("/api/trades", async (IDbConnection db, string? market = null, int limit = 200) =>
{
    limit = Math.Clamp(limit, 1, 1000);
    var rows = await Query<TradeRow>(db, """
        SELECT t.pair, t.side, t.entry, t.exit, t.pnl, t.reason,
               t.opened_at, t.closed_at, s.market
        FROM trn_trade t
        LEFT JOIN trn_signal s ON s.pair = t.pair AND s.signal_ts = t.opened_at
        WHERE (@market IS NULL OR s.market = @market)
        ORDER BY t.closed_at DESC LIMIT @limit
        """, new { market, limit });
    return Results.Ok(rows);
});

app.MapGet("/api/stats", async (IDbConnection db) =>
{
    var s = await Query<StatsRow>(db, """
        SELECT COALESCE(s.market, 'unknown') AS market, count(*) AS trades,
               count(*) FILTER (WHERE t.pnl > 0) AS wins,
               COALESCE(SUM(t.pnl), 0) AS net_pnl,
               CASE WHEN count(*) > 0 THEN count(*) FILTER (WHERE t.pnl > 0)::float / count(*) END AS win_rate,
               CASE WHEN SUM(t.pnl) FILTER (WHERE t.pnl < 0) = 0 THEN NULL
                    ELSE ABS(SUM(t.pnl) FILTER (WHERE t.pnl > 0) / SUM(t.pnl) FILTER (WHERE t.pnl < 0)) END AS profit_factor
        FROM trn_trade t LEFT JOIN trn_signal s ON s.pair = t.pair AND s.signal_ts = t.opened_at
        GROUP BY COALESCE(s.market, 'unknown')
        """);
    return Results.Ok(s);
});

app.MapGet("/api/positions", async (IDbConnection db, string? market = null) =>
{
    // latest snapshot per pair
    var rows = await Query<PositionRow>(db, """
        SELECT DISTINCT ON (market, pair) market, pair, side, entry, sl, tp,
               units, opened_at, snapshot_ts
        FROM trn_position
        WHERE (@market IS NULL OR market = @market)
        ORDER BY market, pair, snapshot_ts DESC
        """, new { market });
    return Results.Ok(rows);
});

app.MapGet("/api/brokers", async (IDbConnection db, string? ticker = null, string? broker = null, int days = 7) =>
{
    days = Math.Clamp(days, 1, 90);
    // net lots/value per broker per ticker over window (buy +, sell -)
    var rows = await Query<BrokerRow>(db, """
        SELECT ticker, broker_code, investor_type,
               SUM(CASE WHEN side = 'BUY' THEN net_lots ELSE -net_lots END) AS net_lots,
               SUM(CASE WHEN side = 'BUY' THEN net_value_idr ELSE -net_value_idr END) AS net_value_idr,
               ROUND(AVG(avg_price) FILTER (WHERE avg_price IS NOT NULL)::numeric, 2) AS avg_price,
               MAX(date) AS last_date
        FROM broker_stock_daily
        WHERE date >= CURRENT_DATE - @days
          AND (@ticker IS NULL OR ticker = @ticker)
          AND (@broker IS NULL OR broker_code = @broker)
        GROUP BY ticker, broker_code, investor_type
        ORDER BY net_value_idr DESC
        """, new { days, ticker, broker });
    return Results.Ok(rows);
});

app.MapGet("/api/orderbook", async (IDbConnection db, string? ticker = null, int limit = 1) =>
{
    // latest depth snapshot per ticker
    var rows = await Query<OrderbookRow>(db, """
        SELECT DISTINCT ON (ticker) ticker, ts, last, imb5, imb10, wall, fnet,
               total_bid_lot, total_ask_lot
        FROM ob_snapshot
        WHERE (@ticker IS NULL OR ticker = @ticker)
        ORDER BY ticker, ts DESC LIMIT @limit
        """, new { ticker, limit });
    return Results.Ok(rows);
});

app.Run();

// ponytail: plain classes (not records) — Dapper needs settable props; records are
//           flaky with this Dapper version on positional ctors. Swap back when bumping Dapper.
class HealthRow { public string Market { get; set; } = ""; public double Equity { get; set; }
    public DateTime Ts { get; set; } public long AgeSeconds { get; set; } }
class EquityRow { public string Market { get; set; } = ""; public double Equity { get; set; }
    public DateTime Ts { get; set; } }
class SignalRow { public string Market { get; set; } = ""; public string Pair { get; set; } = "";
    public string Side { get; set; } = ""; public DateTime SignalTs { get; set; }
    public double? Entry { get; set; } public double? Sl { get; set; } public double? Tp { get; set; }
    public string? Reason { get; set; } public DateTime CreatedAt { get; set; } }
class TradeRow { public string Pair { get; set; } = ""; public string Side { get; set; } = "";
    public double? Entry { get; set; } public double? Exit { get; set; } public double Pnl { get; set; }
    public string Reason { get; set; } = ""; public DateTime? OpenedAt { get; set; }
    public DateTime ClosedAt { get; set; } public string? Market { get; set; } }
class StatsRow { public string Market { get; set; } = ""; public long Trades { get; set; }
    public long Wins { get; set; } public double NetPnl { get; set; }
    public double? WinRate { get; set; } public double? ProfitFactor { get; set; } }
class PositionRow { public string Market { get; set; } = ""; public string Pair { get; set; } = "";
    public string Side { get; set; } = ""; public double? Entry { get; set; } public double? Sl { get; set; }
    public double? Tp { get; set; } public double? Units { get; set; } public DateTime? OpenedAt { get; set; }
    public DateTime SnapshotTs { get; set; } }
class BrokerRow { public string Ticker { get; set; } = ""; public string BrokerCode { get; set; } = "";
    public string InvestorType { get; set; } = ""; public double NetLots { get; set; }
    public double NetValueIdr { get; set; } public double? AvgPrice { get; set; }
    public DateTime LastDate { get; set; } }
class OrderbookRow { public string Ticker { get; set; } = ""; public DateTime Ts { get; set; }
    public double? Last { get; set; } public double? Imb5 { get; set; } public double? Imb10 { get; set; }
    public double? Wall { get; set; } public double? Fnet { get; set; }
    public double? TotalBidLot { get; set; } public double? TotalAskLot { get; set; } }
