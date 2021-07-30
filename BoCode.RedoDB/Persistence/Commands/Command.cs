using System;

namespace BoCode.RedoDB.Persistence.Commands
{
    public enum CommandType
    {
        Method = 0,
        Getter = 1,
        Setter = 2
    }

    /// <summary>
    /// It keeps the Method invocation event, so that it can be redo while restoring class state
    /// </summary>
    [Serializable]
    public record Command(CommandType CommandType, string MemberName, object?[]? Args, CommandContext CommandContext);
}
