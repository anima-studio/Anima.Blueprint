using Moser.Archetype.BuildingBlocks.Domain;
using Moser.Archetype.Web.Abstractions;
using Moser.Archetype.Web.Models;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Moser.Archetype.Web.Services;

public class DemoTransport : IRemoteAccessTransport
{
    private readonly List<AgentInfo> _agents = new();

    public event EventHandler<AgentInfo>? AgentConnected;
    public event EventHandler<string>? AgentDisconnected;
    public TransportState State { get; private set; }

    public async Task<Result> ConnectAsync(CancellationToken cancellationToken = default)
    {
        State = TransportState.Connecting;
        await Task.Delay(500, cancellationToken);

        _agents.AddRange(new[]
        {
            new AgentInfo("DESKTOP-A1", "DESKTOP-A1", "192.168.1.100", "Windows 11 Pro", DateTimeOffset.UtcNow),
            new AgentInfo("LAPTOP-B2", "LAPTOP-B2", "10.0.0.50", "Windows 10 Enterprise", DateTimeOffset.UtcNow.AddSeconds(-30)),
            new AgentInfo("SERVER-C3", "SERVER-C3", "172.16.0.10", "Windows Server 2022", DateTimeOffset.UtcNow.AddMinutes(-2))
        });

        State = TransportState.Connected;
        return Result.Success();
    }

    public Task<Result<IReadOnlyList<AgentInfo>>> GetAgentsAsync()
    {
        return Task.FromResult(Result<IReadOnlyList<AgentInfo>>.Success(_agents));
    }

    public async Task<Result<CommandResponse>> SendCommandAsync(string agentId, AgentCommand command)
    {
        await Task.Delay(Random.Shared.Next(300, 800));

        var response = command.Type switch
        {
            CommandType.Screenshot => new CommandResponse(
                command.CommandId,
                agentId,
                true,
                BinaryData: Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==")),

            CommandType.Shell => new CommandResponse(
                command.CommandId,
                agentId,
                true,
                TextData: "C:\\> systeminfo\nHost Name: " + agentId + "\nOS Name: Windows 11 Pro\nOS Version: 10.0.22631"),

            CommandType.ProcessList => new CommandResponse(
                command.CommandId,
                agentId,
                true,
                TextData: "explorer.exe | 2548 | 145 MB\nchrome.exe | 5632 | 589 MB\nAgent.exe | 8192 | 45 MB"),

            _ => new CommandResponse(command.CommandId, agentId, false, Error: "Unknown command")
        };

        return Result<CommandResponse>.Success(response);
    }
}
