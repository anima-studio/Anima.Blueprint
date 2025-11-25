using Anima.Blueprint.BuildingBlocks.Domain;
using Anima.Blueprint.Web.Models;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Anima.Blueprint.Web.Abstractions;

public interface IRemoteAccessTransport
{
    Task<Result> ConnectAsync(CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<AgentInfo>>> GetAgentsAsync();
    Task<Result<CommandResponse>> SendCommandAsync(string agentId, AgentCommand command);

    event EventHandler<AgentInfo>? AgentConnected;
    event EventHandler<string>? AgentDisconnected;

    TransportState State { get; }
}
