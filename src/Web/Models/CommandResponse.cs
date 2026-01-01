namespace Moser.Archetype.Web.Models;

public record CommandResponse(
    string CommandId,
    string AgentId,
    bool Success,
    byte[]? BinaryData = null,
    string? TextData = null,
    string? Error = null);
