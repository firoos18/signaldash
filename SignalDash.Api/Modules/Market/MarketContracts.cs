namespace SignalDash.Api.Modules.Market;

// ponytail: plain classes (not records) — Dapper needs settable props; records are
//           flaky with this Dapper version on positional ctors. Swap back when bumping Dapper.

/// <summary>Net lots/value per broker per ticker over a window (buy +, sell -).</summary>
public sealed class BrokerRow
{
    public string Ticker { get; set; } = "";
    public string BrokerCode { get; set; } = "";
    public string InvestorType { get; set; } = "";
    public double NetLots { get; set; }
    public double NetValueIdr { get; set; }
    public double? AvgPrice { get; set; }
    public DateTime LastDate { get; set; }
}

/// <summary>Latest orderbook depth snapshot per ticker.</summary>
public sealed class OrderbookRow
{
    public string Ticker { get; set; } = "";
    public DateTime Ts { get; set; }
    public double? Last { get; set; }
    public double? Imb5 { get; set; }
    public double? Imb10 { get; set; }
    public double? Wall { get; set; }
    public double? Fnet { get; set; }
    public double? TotalBidLot { get; set; }
    public double? TotalAskLot { get; set; }
}
