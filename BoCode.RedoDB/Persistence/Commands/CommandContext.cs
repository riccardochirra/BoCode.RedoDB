using System;
using System.Collections.Generic;

namespace BoCode.RedoDB.Persistence.Commands
{
	/// <summary>
	/// CommandContext holds context information about method call, like the time stamp or security aspects
	/// </summary>
	[Serializable]
	public class CommandContext
	{
		public DateTime TimeStamp { get; }
		public List<Guid> TrackedGuids { get; }
		public List<DateTime> TrackedTime { get; }

		public CommandContext(DateTime TimeStamp, List<Guid> TrackedGuids, List<DateTime> TrackedTime)
		{
			this.TimeStamp = TimeStamp;
			this.TrackedGuids = TrackedGuids;
			this.TrackedTime = TrackedTime;
		}
	}
}
