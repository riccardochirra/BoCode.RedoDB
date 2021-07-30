namespace BoCode.RedoDB.RedoableData
{
    /// A redoable system implementing this interface get a decicated instance of a RedoableClock object. You should use 
    /// RedoableClock.Now instead of DateTime.Now whenever you need a new DateTime based on current time in your system's code.
    public interface IDependsOnRedoableClock
    {
        void SetRedoableClock(IRedoableClock clock);
    }
}
