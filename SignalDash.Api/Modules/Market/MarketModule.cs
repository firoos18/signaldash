namespace SignalDash.Api.Modules.Market;

/// <summary>Market module endpoints (broker bandarmology, orderbook depth).</summary>
public static class MarketModule
{
    public static IEndpointRouteBuilder MapMarketEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/brokers", async (IMarketRepository repo, string? ticker = null,
            string? broker = null, int days = 7, CancellationToken ct = default) =>
        {
            days = Math.Clamp(days, 1, 90);
            return Results.Ok(await repo.GetBrokersAsync(ticker, broker, days, ct));
        });

        app.MapGet("/api/orderbook", async (IMarketRepository repo, string? ticker = null,
            int limit = 1, CancellationToken ct = default) =>
        {
            limit = Math.Clamp(limit, 1, 100);
            return Results.Ok(await repo.GetOrderbookAsync(ticker, limit, ct));
        });

        return app;
    }
}
