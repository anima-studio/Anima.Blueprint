using Microsoft.AspNetCore.SignalR;

using System;
using System.Diagnostics.Eventing.Reader;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Moser.Archetype.Web.Hubs;

public class AgentsHub : Hub
{
    private readonly IAgentConnectionManager _connectionManager;

    public override async Task OnConnectedAsync()
    {
        var agentId = Context.GetHttpContext().Request.Headers["X-Agent-Id"];
        var agentKey = Context.GetHttpContext().Request.Headers["X-Agent-Key"];

        if (!await ValidateAgentCredentials(agentId, agentKey))
        {
            Context.Abort();
            return;
        }

        await _connectionManager.RegisterConnection(agentId, Context.ConnectionId);
        await Groups.AddToGroupAsync(Context.ConnectionId, "ConnectedAgents");
        await base.OnConnectedAsync();
    }

    public async Task RegisterAgent(AgentMetadata metadata)
    {
        await _connectionManager.UpdateAgentMetadata(metadata);
        await Clients.Group("AdminPortal").SendAsync("AgentConnected", metadata);
    }

    public async Task Heartbeat(string agentId)
    {
        await _connectionManager.UpdateLastSeen(agentId, DateTimeOffset.UtcNow);
    }

    public async Task SendCommandResult(CommandResult result)
    {
        var sessionId = await _connectionManager.GetActiveSession(Context.ConnectionId);
        await Clients.Group($"Session-{sessionId}").SendAsync("CommandResultReceived", result);
    }
}
