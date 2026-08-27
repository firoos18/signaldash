namespace SignalDash.Api.Modules.Market;

/// <summary>Read access to market data (broker bandarmology, orderbook depth).</summary>
public interface IMarketRepository
{
    Task<IReadOnlyList<BrokerRow>> GetBrokersAsync(string? ticker, string? broker, int days, CancellationToken ct = default);
    Task<IReadOnlyList<OrderbookRow>> GetOrderbookAsync(string? ticker, int limit, CancellationToken ct = default);
}
