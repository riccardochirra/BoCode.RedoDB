using BoCode.RedoDB.Persistence.Infrastructure;

namespace BoCode.RedoDB.Persistence.Commands
{
    public class CommandLogNameProvider : NumberedNameProviderBase, ISnapshotOrLogNameProvider
    {
        public const string LOG_FILE_NAME_FORMAT = "{0:00000000000000000000}.commandlog";

        public string FirstName => GetFirstName(LOG_FILE_NAME_FORMAT);


        public string NewName(string lastName) => GetNewName(lastName, LOG_FILE_NAME_FORMAT);

        public string NewName(long ordinal) => GetNewName(ordinal, LOG_FILE_NAME_FORMAT);
    }
}
