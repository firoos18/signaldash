// ═══════════════════════════════════════════════════════════════════════════
// SignalDash.Api — modular monolith composition root
// Modules: Trading (signals/trades/positions/equity/health/stats),
//          Market  (brokers/orderbook)
// SOLID: each module owns contracts + repository (DIP via interface),
//        endpoints mapped via module extensions. No cross-module deps.
// ═══════════════════════════════════════════════════════════════════════════
using System.Data;
using Dapper;
using Npgsql;
using SignalDash.Api.Modules.Market;
using SignalDash.Api.Modules.Trading;

// snake_case DB columns → camelCase properties
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

// ── module DI ──
builder.Services.AddScoped<ITradingRepository, TradingRepository>();
builder.Services.AddScoped<IMarketRepository, MarketRepository>();

var app = builder.Build();
if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }
app.UseCors("frontend");

// ── module endpoints ──
app.MapTradingEndpoints();
app.MapMarketEndpoints();

app.Run();
