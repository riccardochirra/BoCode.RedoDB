namespace BoCode.RedoDB.Persistence
{
    public interface ISnapshotOrLogNameProvider
    {
        string NewName(string lastName);
        string FirstName { get; }
        string NewName(long ordinal);
    }

}
