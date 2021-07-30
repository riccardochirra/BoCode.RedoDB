using System;
using System.Globalization;
using System.IO;

namespace BoCode.RedoDB.Tester.Infrastructure
{
    public class TesterWithDataPath
    {
        const string TEST_DIRECTORIES = "Test Directories";

        public string NewDataPath
        {
            get => string.Format(CultureInfo.InvariantCulture, "{0}\\{1}", Path.Combine(Environment.CurrentDirectory, TEST_DIRECTORIES), Guid.NewGuid());
        }

        public void EnsureDataPath(string dataPath)
        {
            if (!Directory.Exists(dataPath))
                Directory.CreateDirectory(dataPath);
        }
    }
}
