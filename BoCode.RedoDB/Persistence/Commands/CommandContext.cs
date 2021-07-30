using System;
using System.Collections.Generic;

namespace BoCode.RedoDB.Persistence.Commands
{
    /// <summary>
    /// CommandContext holds context information about method call, like the time stamp or security aspects
    /// </summary>
    [Serializable]
    public record CommandContext(DateTime TimeStamp, List<Guid>? TrackedGuids, List<DateTime>? TrackedTime);
}
