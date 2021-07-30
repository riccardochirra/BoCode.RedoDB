namespace BoCode.RedoDB.RedoableData
{
    /// <summary>
    /// A redoable system implementing this interface get a decicated instance of a RedoableGuid object. You should use 
    /// RedoableGuid.New() instead of Guid.NewGuid() whenever you need a new Guid in your system's code.
    /// </summary>
    public interface IDependsOnRedoableGuid
    {
        public void SetRedoableGuid(IRedoableGuid redoableGuid);
    }
}
