using Microsoft.AspNetCore.SignalR;

namespace Anima.Blueprint.Notifications.API.Features.Agents;

public class AgentsHub : Hub
{
    public async Task SubscribeToAgents(Guid userId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, userId.ToString());
    }
}
