# SignalDash — Trading Bot Monitor

Fullstack monitoring dashboard for the quant-signals trading bots.

## Stack
- **.NET 8 minimal API** (`SignalDash.Api/`) — Dapper + Npgsql, read-only dashboard endpoints
- **NextJS 16** (`web/`) — dark dashboard, equity curve, positions, trades, signals, stats
- **PostgreSQL** — `signaldash` DB (homelab k3s), written by crypto/forex bots hourly
- **Deploy** — Docker images → k3s containerd, GitOps via ArgoCD (homelab-gitops repo)

## API endpoints
```
GET /api/health     bot liveness (last heartbeat per market, stale >90min)
GET /api/equity     equity curve
GET /api/signals    signal history
GET /api/trades     closed trades
GET /api/stats      win rate / profit factor per market
GET /api/positions  current open positions
```

## Access
https://signaldash-homelab (self-signed cert, accept once)

## Local dev
```
cd SignalDash.Api && dotnet run --urls http://0.0.0.0:5080
cd web && NEXT_PUBLIC_API_URL=http://localhost:5080 npm run dev   # next 16: use --webpack on constrained VMs
```

## DB schema
`schema.sql` — signals / trades / equity_snapshots / positions.
Bots write via `src/db_writer.py` (best-effort, never blocks bot runs).

## Known wart
`/api/stats` market shows "unknown" until first signal row exists that matches a trade (join key: pair + opened_at).
