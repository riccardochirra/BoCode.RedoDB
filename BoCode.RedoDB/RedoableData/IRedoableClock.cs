using System;

namespace BoCode.RedoDB.RedoableData
{
    /// <summary>
    /// The redoable clock is used to enable predeterministic usage of DateTime.Now
    /// </summary>
    public interface IRedoableClock : IRedoableData<DateTime>
    {
        DateTime Now { get; }
    }
}
