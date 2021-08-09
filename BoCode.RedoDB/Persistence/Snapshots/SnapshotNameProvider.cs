using BoCode.RedoDB.Persistence.Infrastructure;

namespace BoCode.RedoDB.Persistence.Snapshots
{

    public class SnapshotNameProvider : NumberedNameProviderBase, ISnapshotOrLogNameProvider
    {
        public const string SNAPSHOT_FILE_NAME_FORMAT = "{0:00000000000000000000}.snapshot";

        public string FirstName => base.GetFirstName(SNAPSHOT_FILE_NAME_FORMAT);

        public string NewName(string lastName) => base.GetNewName(lastName, SNAPSHOT_FILE_NAME_FORMAT);

        public string NewName(long ordinal) => base.GetNewName(ordinal, SNAPSHOT_FILE_NAME_FORMAT);
    }
}
