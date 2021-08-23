using System;
using System.Threading.Tasks;

namespace BoCode.RedoDB.Persistence.Snapshots
{
    public interface ISnapshotManager<T> : IDisposable
    {
        string LastSnapshot { get; }

        void TakeSnapshot();

        Task<T> RecoverFromSnapshot();
        T GetLastDeserialization();
    }
}
