const API = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5080";

export type Bot = { market: string; equity: number; ts: string; ageSeconds: number; stale: boolean };
export type Health = { status: string; bots: Bot[] };
export type EquityPoint = { market: string; equity: number; ts: string };
export type Signal = { market: string; pair: string; side: string; signalTs: string; entry: number | null; sl: number | null; tp: number | null; reason: string | null };
export type Trade = { pair: string; side: string; entry: number | null; exit: number | null; pnl: number; reason: string; openedAt: string | null; closedAt: string; market: string | null };
export type Stats = { market: string; trades: number; wins: number; netPnl: number; winRate: number | null; profitFactor: number | null };
export type Position = { market: string; pair: string; side: string; entry: number | null; sl: number | null; tp: number | null; units: number | null; openedAt: string | null };
export type Broker = { ticker: string; brokerCode: string; investorType: string; netLots: number; netValueIdr: number; avgPrice: number | null; lastDate: string };
export type Orderbook = { ticker: string; ts: string; last: number | null; imb5: number | null; imb10: number | null; wall: number | null; fnet: number | null; totalBidLot: number | null; totalAskLot: number | null };

async function get<T>(path: string): Promise<T> {
  const r = await fetch(`${API}${path}`, { cache: "no-store" });
  if (!r.ok) throw new Error(`${path}: HTTP ${r.status}`);
  return r.json();
}

export const api = {
  health: () => get<Health>("/api/health"),
  equity: () => get<EquityPoint[]>("/api/equity"),
  signals: (limit = 50) => get<Signal[]>(`/api/signals?limit=${limit}`),
  trades: (limit = 100) => get<Trade[]>(`/api/trades?limit=${limit}`),
  stats: () => get<Stats[]>("/api/stats"),
  positions: () => get<Position[]>("/api/positions"),
  brokers: (days = 7) => get<Broker[]>(`/api/brokers?days=${days}`),
  orderbook: () => get<Orderbook[]>("/api/orderbook"),
};
