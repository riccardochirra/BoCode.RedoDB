using BoCode.RedoDB.Compensation;
using System.Collections.Generic;

namespace BoCode.RedoDB.Persistence.Commands
{
    /// <summary>
    /// The CommandsManager tracks Commands since the last TakeSnapshot action flushing commands to 
    /// the configured command log immediately. When a snapshot is taken, the CommandManager closes log (the internal list of 
    /// commands is cleared) and a new log is started.
    /// </summary>
    public class CommandsManager<T> : ICommandsManager<T> where T : class, new()
    {
        //reflects the current log.
        List<Command> _commands = new List<Command>();
        //commands processed since engine start.
        List<Command> _processedCommands = new List<Command>();

        private ICommandAdapter _adapter;
        private CompensationManager<T>? _compensationManager;

        public CommandsManager(ICommandAdapter adapter)
        {
            _adapter = adapter;
            RecoveringLog = new List<Command>();
            RecoveringLogFaultyCommands = new List<Command>();
        }

        public int Count { get => _commands.Count; }

        public IEnumerable<Command> Log => _commands;

        public IEnumerable<Command> ProcessedCommands => _processedCommands;

        /// <summary>
        /// Contains all commands used to recover the initial state
        /// </summary>
        public IEnumerable<Command> RecoveringLog { get; set; }

        /// <summary>
        /// Contains all faulty commands while recovering the initial state
        /// </summary>
        public IEnumerable<Command> RecoveringLogFaultyCommands { get; set; }

        public void AddCommand(Command command)
        {
            _commands.Add(command);
            _processedCommands.Add(command);
            WriteCommand(command);
        }

        private void WriteCommand(Command command)
        {
            _adapter.WriteCommandAsync(command);
        }

        public void AssertBuildReady()
        {
            //do nothing, build ready!
        }

        public void NoPersistence()
        {
            //TODO: do nothing for the moment, to be implemented later.
        }

        public void CloseCommandlog()
        {
            _adapter.CloseCommandLog();
            _commands.Clear();
        }

        public void Dispose()
        {
            _adapter.Dispose();
        }

        public void SetCompensationManager(CompensationManager<T> compensationManager)
        {
            _compensationManager = compensationManager;
        }
    }
}
