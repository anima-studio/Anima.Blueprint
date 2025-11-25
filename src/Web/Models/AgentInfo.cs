using System;

namespace Anima.Blueprint.Web.Models;

public record AgentInfo(
    string AgentId,
    string Hostname,
    string IpAddress,
    string OsVersion,
    DateTimeOffset LastSeen);
