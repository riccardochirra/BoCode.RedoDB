using BoCode.RedoDB.Builder;
using BoCode.RedoDB.Compensation;
using System;
using System.Collections.Generic;

namespace BoCode.RedoDB.Persistence.Commands
{
    public interface ICommandsManager<T> : IBuilderComponent, IDisposable where T : class, new()
    {
        IEnumerable<Command> Log { get; }
        IEnumerable<Command> RecoveringLog { get; set; }
        IEnumerable<Command> RecoveringLogFaultyCommands { get; set; }
        IEnumerable<Command> ProcessedCommands { get; }

        void AddCommand(Command command);
        void CloseCommandlog();
        void SetCompensationManager(CompensationManager<T> compensationManager);

    }
}
