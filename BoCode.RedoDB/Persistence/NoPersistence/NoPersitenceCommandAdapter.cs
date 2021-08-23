using BoCode.RedoDB.Persistence.Commands;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BoCode.RedoDB.Persistence.NoPersistence
{
    public class NoPersitenceCommandAdapter : ICommandAdapter
    {
        public string LastSnapshotName { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public IEnumerable<Commandlog> RecoveringLogs => throw new NotImplementedException();

        public void AssertBuildReady()
        {
            //do nothing. allways ready
        }

        public void CloseCommandLog()
        {
            //do nothing.
        }

        public void Dispose()
        {
            //nothing to dispose.
        }

        public void NoPersistence()
        {
            //yes, allways for this adapter.
        }

        public Task WriteCommandAsync(Command command)
        {
            //nothing to do.
            return Task.CompletedTask;
        }
    }
}
