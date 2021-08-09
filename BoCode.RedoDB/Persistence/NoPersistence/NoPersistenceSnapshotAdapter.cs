using System;
using System.Threading.Tasks;
using BoCode.RedoDB.Persistence.Snapshots;

namespace BoCode.RedoDB.Persistence.NoPersistence
{
    public class NoPersistenceSnapshotAdapter<T> : ISnapshotAdapter<T>
    {
        public string LastSnapshot => throw new NotImplementedException();

        public void AssertBuildReady()
        {
            //do nothing, allways ready.
        }

        public T? Deserialize()
        {
            //nothing to serialize
            return default;
        }

        public Task<T?> DeserializeAsync()
        {
            //do nothing.
#pragma warning disable CS8603 // Possible null reference return.
            return null;
#pragma warning restore CS8603 // Possible null reference return.
        }

        public void Dispose()
        {
            //nothing to dispose.
        }

        public T? GetLastSnapshot()
        {
            throw new NotImplementedException();
        }

        public void NoPersistence()
        {
            //yes, by default for this adapter.
        }

        public void Serialize(T redoable)
        {
            //nothing to serialize.
        }
    }
}
