using System.Collections.Generic;

namespace Anima.Blueprint.Web.Models;

public record AgentCommand(
    string CommandId,
    CommandType Type,
    Dictionary<string, string>? Parameters = null);
