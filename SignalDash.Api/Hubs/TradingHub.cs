using Microsoft.AspNetCore.SignalR;

namespace SignalDash.Api.Hubs;

public sealed class TradingHub : Hub
{
    public async Task BroadcastUpdate()
    {
        await Clients.All.SendAsync("DashboardUpdate");
    }
}
