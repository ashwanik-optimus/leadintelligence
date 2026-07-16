using Microsoft.AspNetCore.SignalR;

namespace LeadPortal.Hubs;

public class LeadsHub : Hub
{
    // Clients connect here to receive leadCreated and leadUpdated events.
    // Server pushes via IHubContext<LeadsHub> — no client-to-server methods needed.
}
