using System;
using System.Globalization;
using System.IO;
using Xunit.Abstractions;

namespace BoCode.RedoDB.Tester.Infrastructure
{
    public class TesterWithDataPath
    {
        private readonly ITestOutputHelper _output;

        const string TEST_DIRECTORIES = "Test Directories";

        public TesterWithDataPath(ITestOutputHelper output)
        {
            _output = output;
        }

        public string NewDataPath
        {
            get
            {
                var dataPath = string.Format(CultureInfo.InvariantCulture, "{0}\\{1}", Path.Combine(Environment.CurrentDirectory, TEST_DIRECTORIES), Guid.NewGuid());
                _output.WriteLine(dataPath);
                return dataPath;   
            }
        }

        public void EnsureDataPath(string dataPath)
        {
            if (!Directory.Exists(dataPath))
                Directory.CreateDirectory(dataPath);
        }
    }
}
