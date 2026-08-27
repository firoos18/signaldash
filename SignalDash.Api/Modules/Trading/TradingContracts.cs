namespace SignalDash.Api.Modules.Trading;

// ponytail: plain classes (not records) — Dapper needs settable props; records are
//           flaky with this Dapper version on positional ctors. Swap back when bumping Dapper.

/// <summary>Last equity snapshot per market = last bot heartbeat.</summary>
public sealed class HealthRow
{
    public string Market { get; set; } = "";
    public double Equity { get; set; }
    public DateTime Ts { get; set; }
    public long AgeSeconds { get; set; }
}

public sealed class EquityRow
{
    public string Market { get; set; } = "";
    public double Equity { get; set; }
    public DateTime Ts { get; set; }
}

public sealed class SignalRow
{
    public string Market { get; set; } = "";
    public string Pair { get; set; } = "";
    public string Side { get; set; } = "";
    public DateTime SignalTs { get; set; }
    public double? Entry { get; set; }
    public double? Sl { get; set; }
    public double? Tp { get; set; }
    public string? Reason { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class TradeRow
{
    public string Pair { get; set; } = "";
    public string Side { get; set; } = "";
    public double? Entry { get; set; }
    public double? Exit { get; set; }
    public double Pnl { get; set; }
    public string Reason { get; set; } = "";
    public DateTime? OpenedAt { get; set; }
    public DateTime ClosedAt { get; set; }
    public string? Market { get; set; }
}

public sealed class StatsRow
{
    public string Market { get; set; } = "";
    public long Trades { get; set; }
    public long Wins { get; set; }
    public double NetPnl { get; set; }
    public double? WinRate { get; set; }
    public double? ProfitFactor { get; set; }
}

public sealed class PositionRow
{
    public string Market { get; set; } = "";
    public string Pair { get; set; } = "";
    public string Side { get; set; } = "";
    public double? Entry { get; set; }
    public double? Sl { get; set; }
    public double? Tp { get; set; }
    public double? Units { get; set; }
    public DateTime? OpenedAt { get; set; }
    public DateTime SnapshotTs { get; set; }
}
