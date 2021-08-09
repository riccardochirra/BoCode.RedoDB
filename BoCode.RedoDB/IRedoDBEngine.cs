using BoCode.RedoDB.Compensation;
using BoCode.RedoDB.Persistence.Commands;
using System;
using System.Collections.Generic;
using BoCode.RedoDB.Interception;

namespace BoCode.RedoDB
{
    public interface IRedoDBEngine<T> : IDisposable where T : class, new()
    {
        ICommandsManager<T> Commands { get; }
        IInterceptions Instructions { get; }

        void TakeSnapshot();
    }

    public interface IRedoEngineInternal<T> where T : class, new()
    {
        void SetCommandsManager(ICommandsManager<T> commandManager);
        void SetInterceptionsManager(IInterceptions interceptionsManager);
        void DeactivatePersistence();
        void SetCompensationManager(CompensationManager<T> compensationManager);
        void SetRecoveredCommands(List<Command> recoveredCommands);
        void SetRecoveredFaultyCommands(IEnumerable<Command> faultyCommands);
    }
}
