"use client";

import { useEffect, useState } from "react";
import {
  LineChart, Line, XAxis, YAxis, Tooltip, ResponsiveContainer, CartesianGrid, Legend,
} from "recharts";
import { IconActivity, IconTrendingUp, IconWallet, IconChartLine } from "@tabler/icons-react";
import { api, type Health, type EquityPoint, type Signal, type Trade, type Stats, type Position } from "@/lib/api";

// ponytail: single client page, no RSC/data layer. Fine for one user; split into
//           server components + suspense if this ever serves many viewers.
export default function Dashboard() {
  const [health, setHealth] = useState<Health | null>(null);
  const [equity, setEquity] = useState<EquityPoint[]>([]);
  const [signals, setSignals] = useState<Signal[]>([]);
  const [trades, setTrades] = useState<Trade[]>([]);
  const [stats, setStats] = useState<Stats[]>([]);
  const [positions, setPositions] = useState<Position[]>([]);
  const [err, setErr] = useState<string | null>(null);

  const load = () => {
    Promise.all([api.health(), api.equity(), api.signals(), api.trades(), api.stats(), api.positions()])
      .then(([h, e, s, t, st, p]) => {
        setHealth(h); setEquity(e); setSignals(s); setTrades(t); setStats(st); setPositions(p); setErr(null);
      })
      .catch((e: Error) => setErr(e.message));
  };
  useEffect(() => { load(); const id = setInterval(load, 30000); return () => clearInterval(id); }, []);

  const totalStats = stats.reduce((a, s) => ({ trades: a.trades + s.trades, wins: a.wins + s.wins, netPnl: a.netPnl + s.netPnl }), { trades: 0, wins: 0, netPnl: 0 });
  const winRate = totalStats.trades ? (totalStats.wins / totalStats.trades) * 100 : null;
  const openCount = positions.length;

  return (
    <main className="min-h-[100dvh] bg-zinc-950 text-zinc-100">
      <div className="mx-auto max-w-[1400px] px-6 py-8">
        {/* header */}
        <header className="mb-8 flex items-center justify-between">
          <div>
            <h1 className="text-2xl font-semibold tracking-tight">SignalDash</h1>
            <p className="text-sm text-zinc-500">Trading bot monitor</p>
          </div>
          <div className="flex items-center gap-3">
            {health?.bots.map((b) => (
              <span key={b.market} className={`flex items-center gap-2 rounded-full border px-3 py-1 text-xs font-medium ${b.stale ? "border-red-500/40 text-red-400" : "border-emerald-500/40 text-emerald-400"}`}>
                <span className={`h-1.5 w-1.5 rounded-full ${b.stale ? "bg-red-500" : "bg-emerald-500"}`} />
                {b.market} {b.stale ? `${Math.round(b.ageSeconds / 60)}m stale` : "ok"}
              </span>
            ))}
            {err && <span className="text-xs text-red-400">API: {err}</span>}
          </div>
        </header>

        {/* KPI cards */}
        <section className="mb-8 grid grid-cols-2 gap-4 md:grid-cols-4">
          <Kpi icon={<IconWallet size={18} />} label="Equity" value={`$${health?.bots.reduce((a, b) => a + b.equity, 0).toFixed(2) ?? "—"}`} />
          <Kpi icon={<IconActivity size={18} />} label="Win rate" value={winRate === null ? "—" : `${winRate.toFixed(1)}%`} sub={`${totalStats.wins}/${totalStats.trades} trades`} />
          <Kpi icon={<IconChartLine size={18} />} label="Open positions" value={String(openCount)} />
          <Kpi icon={<IconTrendingUp size={18} />} label="Net PnL" value={`$${totalStats.netPnl.toFixed(2)}`} positive={totalStats.netPnl >= 0} />
        </section>

        {/* equity chart */}
        <section className="mb-8 rounded-xl border border-zinc-800 bg-zinc-900/50 p-5">
          <h2 className="mb-4 text-sm font-medium text-zinc-400">Equity curve</h2>
          {equity.length === 0 ? <Empty label="No equity snapshots yet. Bots write one every run." /> : (
            <ResponsiveContainer width="100%" height={280}>
              <LineChart data={equity} margin={{ top: 5, right: 10, bottom: 5, left: 0 }}>
                <CartesianGrid stroke="#27272a" strokeDasharray="3 3" />
                <XAxis dataKey="ts" tick={{ fill: "#71717a", fontSize: 11 }} tickFormatter={(v: string) => v.slice(5, 16)} minTickGap={40} />
                <YAxis tick={{ fill: "#71717a", fontSize: 11 }} domain={["auto", "auto"]} width={60} />
                <Tooltip contentStyle={{ background: "#18181b", border: "1px solid #3f3f46", borderRadius: 8, fontSize: 12 }} labelFormatter={(v) => String(v)} />
                <Legend wrapperStyle={{ fontSize: 12 }} />
                <Line type="monotone" dataKey="equity" stroke="#10b981" strokeWidth={2} dot={false} name="Equity" />
              </LineChart>
            </ResponsiveContainer>
          )}
        </section>

        <div className="grid gap-8 lg:grid-cols-2">
          {/* positions */}
          <section className="rounded-xl border border-zinc-800 bg-zinc-900/50 p-5">
            <h2 className="mb-4 text-sm font-medium text-zinc-400">Open positions</h2>
            {positions.length === 0 ? <Empty label="No open positions." /> : (
              <Table head={["Pair", "Side", "Entry", "SL", "TP", "Units"]}>
                {positions.map((p) => (
                  <tr key={p.pair} className="border-t border-zinc-800/60">
                    <td className="py-2 font-mono text-sm">{p.pair}</td>
                    <td><SideBadge side={p.side} /></td>
                    <td className="font-mono text-sm">{p.entry?.toFixed(4)}</td>
                    <td className="font-mono text-sm text-red-400">{p.sl?.toFixed(4)}</td>
                    <td className="font-mono text-sm text-emerald-400">{p.tp?.toFixed(4)}</td>
                    <td className="font-mono text-sm">{p.units?.toFixed(4)}</td>
                  </tr>
                ))}
              </Table>
            )}
          </section>

          {/* stats per market */}
          <section className="rounded-xl border border-zinc-800 bg-zinc-900/50 p-5">
            <h2 className="mb-4 text-sm font-medium text-zinc-400">Performance</h2>
            {stats.length === 0 ? <Empty label="No closed trades yet." /> : (
              <Table head={["Market", "Trades", "WR", "Net PnL", "PF"]}>
                {stats.map((s) => (
                  <tr key={s.market} className="border-t border-zinc-800/60">
                    <td className="py-2 text-sm capitalize">{s.market}</td>
                    <td className="font-mono text-sm">{s.trades}</td>
                    <td className="font-mono text-sm">{s.winRate === null ? "—" : `${(s.winRate * 100).toFixed(0)}%`}</td>
                    <td className={`font-mono text-sm ${s.netPnl >= 0 ? "text-emerald-400" : "text-red-400"}`}>{s.netPnl.toFixed(2)}</td>
                    <td className="font-mono text-sm">{s.profitFactor === null ? "—" : s.profitFactor.toFixed(2)}</td>
                  </tr>
                ))}
              </Table>
            )}
          </section>

          {/* trades */}
          <section className="rounded-xl border border-zinc-800 bg-zinc-900/50 p-5">
            <h2 className="mb-4 text-sm font-medium text-zinc-400">Recent trades</h2>
            {trades.length === 0 ? <Empty label="No trades yet." /> : (
              <Table head={["Pair", "Side", "Reason", "PnL", "Closed"]}>
                {trades.slice(0, 10).map((t, i) => (
                  <tr key={i} className="border-t border-zinc-800/60">
                    <td className="py-2 font-mono text-sm">{t.pair}</td>
                    <td><SideBadge side={t.side} /></td>
                    <td className="text-sm">{t.reason}</td>
                    <td className={`font-mono text-sm ${t.pnl >= 0 ? "text-emerald-400" : "text-red-400"}`}>{t.pnl >= 0 ? "+" : ""}{t.pnl.toFixed(2)}</td>
                    <td className="font-mono text-xs text-zinc-500">{t.closedAt.slice(0, 16)}</td>
                  </tr>
                ))}
              </Table>
            )}
          </section>

          {/* signals */}
          <section className="rounded-xl border border-zinc-800 bg-zinc-900/50 p-5">
            <h2 className="mb-4 text-sm font-medium text-zinc-400">Recent signals</h2>
            {signals.length === 0 ? <Empty label="No signals yet. ICT setups are ~1/week per pair." /> : (
              <Table head={["Market", "Pair", "Side", "Entry", "Time"]}>
                {signals.slice(0, 10).map((s, i) => (
                  <tr key={i} className="border-t border-zinc-800/60">
                    <td className="py-2 text-sm capitalize">{s.market}</td>
                    <td className="font-mono text-sm">{s.pair}</td>
                    <td><SideBadge side={s.side} /></td>
                    <td className="font-mono text-sm">{s.entry?.toFixed(4)}</td>
                    <td className="font-mono text-xs text-zinc-500">{s.signalTs.slice(0, 16)}</td>
                  </tr>
                ))}
              </Table>
            )}
          </section>
        </div>

        <footer className="mt-10 border-t border-zinc-800/60 pt-4 text-xs text-zinc-600">
          Auto-refresh 30s. Data: Postgres via SignalDash API.
        </footer>
      </div>
    </main>
  );
}

function Kpi({ icon, label, value, sub, positive }: { icon: React.ReactNode; label: string; value: string; sub?: string; positive?: boolean }) {
  return (
    <div className="rounded-xl border border-zinc-800 bg-zinc-900/50 p-4">
      <div className="mb-2 flex items-center gap-2 text-zinc-500">{icon}<span className="text-xs">{label}</span></div>
      <div className={`font-mono text-2xl tracking-tight ${positive === undefined ? "" : positive ? "text-emerald-400" : "text-red-400"}`}>{value}</div>
      {sub && <div className="mt-1 text-xs text-zinc-600">{sub}</div>}
    </div>
  );
}

function SideBadge({ side }: { side: string }) {
  const long = side === "LONG";
  return <span className={`rounded px-1.5 py-0.5 text-[11px] font-medium ${long ? "bg-emerald-500/15 text-emerald-400" : "bg-red-500/15 text-red-400"}`}>{side}</span>;
}

function Table({ head, children }: { head: string[]; children: React.ReactNode }) {
  return (
    <div className="overflow-x-auto">
      <table className="w-full text-left">
        <thead><tr>{head.map((h) => <th key={h} className="pb-2 text-xs font-medium text-zinc-500">{h}</th>)}</tr></thead>
        <tbody>{children}</tbody>
      </table>
    </div>
  );
}

function Empty({ label }: { label: string }) {
  return <div className="rounded-lg border border-dashed border-zinc-800 px-4 py-8 text-center text-sm text-zinc-600">{label}</div>;
}
