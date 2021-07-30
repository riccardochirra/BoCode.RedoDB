using System.Collections.Generic;

namespace BoCode.RedoDB.RedoableData
{
    /// <summary>
    /// The IRedoableData defines the interface of a redoable data tracker. A redoable data tracker offers 
    /// the ability to create new values in the redoable system and track new values so that while redoing (recovering)
    /// exactly the same values will be reapplied whenever the new method is required for a tracked data type.
    /// 
    /// Example: my redoable system calls Guid.New internally. This would cause the call to be not predetrministic, as the same command
    /// executed a second time would generate another Guid. Thus we must have a way to be sure, that if we redo a command requiring the generation of a guid, 
    /// it receives the same Guid he was genereting on first execution.
    /// </summary>
    public interface IRedoableData<T>
    {
        T New();
        IEnumerable<T> Tracked { get; }

        void ClearTracking();
        void Redoing(List<T> repeatValues);
    }
}
