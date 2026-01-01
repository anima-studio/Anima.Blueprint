using System.Collections.Generic;

namespace Moser.Archetype.Web.Models;

public record AgentCommand(
    string CommandId,
    CommandType Type,
    Dictionary<string, string>? Parameters = null);
