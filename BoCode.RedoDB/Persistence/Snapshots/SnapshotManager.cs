using System;
using System.Threading.Tasks;

namespace BoCode.RedoDB.Persistence.Snapshots
{
    /// <summary>
    /// The SnapshotManager is responsible for the redoable's object graph serialization and deserialization.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SnapshotManager<T> : ISnapshotManager<T>
    {
        Func<T> _redoable;
        ISnapshotAdapter<T> _adapter;
        public SnapshotManager(Func<T> redoable, ISnapshotAdapter<T> adapter)
        {
            _redoable = redoable;
            _adapter = adapter;
        }

        public string LastSnapshot => _adapter?.LastSnapshot;

        public void TakeSnapshot()
        {
            _adapter.Serialize(_redoable());
        }

        public async Task<T> RecoverFromSnapshot()
        {
            return await _adapter.DeserializeAsync();
        }

        public void Dispose()
        {
            _adapter.Dispose();
        }

        public T GetLastDeserialization()
        {
            return _adapter.GetLastSnapshot();
        }
    }
}
