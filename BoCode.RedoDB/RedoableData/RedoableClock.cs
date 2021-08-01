using System;

namespace BoCode.RedoDB.RedoableData
{
    /// <summary>
    /// In a redoable system you can't use DateTime.Now because while recovering a command using
    /// DateTime.Now you would get another result as the original one. RedoableClock is able to return
    /// the original value while recovering the command or execute DateTime.Now internally and save the value for 
    /// recovering purposes. If your system need to get the DateTime.Now value, use RedoableClock.Now instead. 
    /// If your redoable system implements the interface IDependsOnRedoableClock, when you build the RedoDBEngine for your
    /// system a dedicated instance of RedoableClock is injected in your system. Please save this instance and use it instead of DateTime.Now.
    /// </summary>
    public class RedoableClock : RedoableData<DateTime>, IRedoableClock
    {
        public RedoableClock() : base(() => DateTime.Now)
        {

        }

        public DateTime Now => base.New();
    }
}
