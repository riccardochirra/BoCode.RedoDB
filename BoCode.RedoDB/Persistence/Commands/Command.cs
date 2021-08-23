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
    public class Command
    {
	    public CommandType CommandType { get; }
	    public string MemberName { get; }
	    public object[] Args { get; }
	    public CommandContext CommandContext { get; }

	    public Command(CommandType CommandType, string MemberName, object[] Args, CommandContext CommandContext)
	    {
		    this.CommandType = CommandType;
		    this.MemberName = MemberName;
		    this.Args = Args;
		    this.CommandContext = CommandContext;
	    }
    }
}
