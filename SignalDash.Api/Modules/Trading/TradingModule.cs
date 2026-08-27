namespace SignalDash.Api.Modules.Trading;

/// <summary>Trading module endpoints (signals, trades, positions, equity, health, stats).</summary>
public static class TradingModule
{
    public static IEndpointRouteBuilder MapTradingEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/health", async (ITradingRepository repo, CancellationToken ct) =>
        {
            var h = await repo.GetHealthAsync(ct);
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

        app.MapGet("/api/equity", async (ITradingRepository repo, string? market = null, CancellationToken ct = default)
            => Results.Ok(await repo.GetEquityAsync(market, ct)));

        app.MapGet("/api/signals", async (ITradingRepository repo, string? market = null,
            string? pair = null, int limit = 100, CancellationToken ct = default) =>
        {
            limit = Math.Clamp(limit, 1, 1000);
            return Results.Ok(await repo.GetSignalsAsync(market, pair, limit, ct));
        });

        app.MapGet("/api/trades", async (ITradingRepository repo, string? market = null,
            int limit = 200, CancellationToken ct = default) =>
        {
            limit = Math.Clamp(limit, 1, 1000);
            return Results.Ok(await repo.GetTradesAsync(market, limit, ct));
        });

        app.MapGet("/api/stats", async (ITradingRepository repo, CancellationToken ct = default)
            => Results.Ok(await repo.GetStatsAsync(ct)));

        app.MapGet("/api/positions", async (ITradingRepository repo, string? market = null, CancellationToken ct = default)
            => Results.Ok(await repo.GetPositionsAsync(market, ct)));

        return app;
    }
}
