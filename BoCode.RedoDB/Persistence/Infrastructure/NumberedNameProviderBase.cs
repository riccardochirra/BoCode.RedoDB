using System.Linq;

namespace BoCode.RedoDB.Persistence.Infrastructure
{
    abstract public class NumberedNameProviderBase
    {
        public long GetIndexFromName(string name)
        {
            string firstToken = name.Split('.').First();
            return long.Parse(firstToken);
        }

        protected string GetNewName(string lastName, string format)
        {
            long value = GetIndexFromName(lastName) + 1;
            return string.Format(format, value);
        }

        protected string GetNewName(long ordinal, string format)
        {
            return string.Format(format, ++ordinal);
        }

        protected string GetFirstName(string format) => string.Format(format, 1);
    }
}
