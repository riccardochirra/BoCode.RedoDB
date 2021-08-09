namespace BoCode.RedoDB.Persistence.Infrastructure
{
    public interface ISnapshotOrLogNameProvider
    {
        string NewName(string lastName);
        string FirstName { get; }
        string NewName(long ordinal);
    }

}
