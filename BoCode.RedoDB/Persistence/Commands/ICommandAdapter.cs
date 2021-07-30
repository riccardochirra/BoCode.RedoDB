
using BoCode.RedoDB.Builder;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BoCode.RedoDB.Persistence.Commands
{
    /// <summary>
    /// This interface must be implemented by the command persistence adapter
    /// </summary>
    public interface ICommandAdapter : IBuilderComponent, IDisposable
    {
        Task WriteCommandAsync(Command command);
        void CloseCommandLog();
        string? LastSnapshotName { get; set; }
        IEnumerable<Commandlog> RecoveringLogs { get; }
    }
}
