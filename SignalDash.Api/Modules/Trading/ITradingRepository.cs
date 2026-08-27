namespace SignalDash.Api.Modules.Trading;

/// <summary>Read access to trading data (signals, trades, positions, equity).</summary>
public interface ITradingRepository
{
    Task<IReadOnlyList<HealthRow>> GetHealthAsync(CancellationToken ct = default);
    Task<IReadOnlyList<EquityRow>> GetEquityAsync(string? market, CancellationToken ct = default);
    Task<IReadOnlyList<SignalRow>> GetSignalsAsync(string? market, string? pair, int limit, CancellationToken ct = default);
    Task<IReadOnlyList<TradeRow>> GetTradesAsync(string? market, int limit, CancellationToken ct = default);
    Task<IReadOnlyList<StatsRow>> GetStatsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<PositionRow>> GetPositionsAsync(string? market, CancellationToken ct = default);
}
