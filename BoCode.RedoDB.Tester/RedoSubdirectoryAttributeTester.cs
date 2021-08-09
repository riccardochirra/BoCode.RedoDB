using BoCode.RedoDB.RedoableSystem;
using BoCode.RedoDB.Tester.Infrastructure;
using FluentAssertions;
using System.IO;
using System.Linq;
using BoCode.RedoDB.Builder;
using Xunit;

namespace BoCode.RedoDB.Tester
{
    public class RedoSubdirectoryAttributeTester : TesterWithDataPath
    {
        [Fact(DisplayName = "GIVEN a redoable system is marked with the RedoSubdirectoryAttribute " +
                            "WHEN the commandlog is created " +
                            "THEN it is created in the specified subdirectory of the datapath.")]
        public void Test1()
        {
            //ARRANGE
            string dataPath = NewDataPath;
            //first instance
            RedoDBEngineBuilder<TestSubdirectorySystem, ITestSubdirectorySystem> builder = new();
            builder.WithJsonAdapters(dataPath);
            ITestSubdirectorySystem system = builder.Build();
            using (var engine = RedoDBEngine<TestSubdirectorySystem>.GetEngine(system))
            {
                system.DoSomething();
            }

            //ASSERT
            Directory.Exists(Path.Combine(dataPath, "Subdirectory")).Should().Be(true);
            Directory.GetFiles(Path.Combine(dataPath, "Subdirectory")).Should().HaveCount(1);
        }

        [Fact(DisplayName = "GIVEN a redoable system is marked with the RedoSubdirectoryAttribute " +
                            "WHEN the snapshot is created " +
                            "THEN it is created in the specified subdirectory of the datapath.")]
        public void Test2()
        {
            //ARRANGE
            string dataPath = NewDataPath;
            //first instance
            RedoDBEngineBuilder<TestSubdirectorySystem, ITestSubdirectorySystem> builder = new();
            builder.WithJsonAdapters(dataPath);
            ITestSubdirectorySystem system = builder.Build();
            using (var engine = RedoDBEngine<TestSubdirectorySystem>.GetEngine(system))
            {
                system.DoSomething();
                engine.TakeSnapshot();
            }

            //ASSERT
            var files = Directory.GetFiles(Path.Combine(dataPath, "Subdirectory")).Select(x => new FileInfo(x).Name);
            files.Count().Should().Be(2);
            files.ElementAt(0).Should().Be("00000000000000000001.commandlog");
            files.ElementAt(1).Should().Be("00000000000000000001.snapshot");
        }
    }
}
