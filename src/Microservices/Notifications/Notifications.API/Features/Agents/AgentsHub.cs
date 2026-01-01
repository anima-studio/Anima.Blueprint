using Microsoft.AspNetCore.SignalR;

namespace Moser.Archetype.Notifications.API.Features.Agents;

public class AgentsHub : Hub
{
    public async Task SubscribeToAgents(Guid userId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, userId.ToString());
    }
}
