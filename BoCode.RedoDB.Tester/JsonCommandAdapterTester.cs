using BoCode.RedoDB.Persistence.Commands;
using BoCode.RedoDB.Tester.Infrastructure;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace BoCode.RedoDB.Tester
{
    [Collection("Test Directory Collection")]
    public class JsonCommandAdapterTester : TesterWithDataPath
    {
        [Fact(DisplayName = "A command can be serialized")]
        public async Task Test1()
        {
            //ARRANGE
            var dataPath = NewDataPath;
            using JsonCommandAdapter adapter = new JsonCommandAdapter(new(dataPath), new CommandLogNameProvider());
            Command command = new Command(
                CommandType.Method,
                "Test",
                new object[] { new DateTime(2021, 7, 13) },
                null
                );

            //ACT
            await adapter.WriteCommandAsync(command);
            adapter.CloseCommandLog();

            //ASSERT
            var files = Directory.GetFiles(dataPath);
            files.Count().Should().Be(1);
            files.First().Should().EndWith("00000000000000000001.commandlog");
            string text = File.ReadAllText(Path.Combine(dataPath, "00000000000000000001.commandlog"));
            text.Length.Should().BeGreaterThan(0);
        }

        [Fact(DisplayName = "Write to commands to log.")]
        public async Task Test2()
        {
            //ARRANGE
            var dataPath = NewDataPath;
            JsonCommandAdapter adapter = new JsonCommandAdapter(new(dataPath), new CommandLogNameProvider());
            //ACT
            Command command = new Command(CommandType.Method, "Command1", new object[] { new DateTime(2021, 7, 13) }, null);
            await adapter.WriteCommandAsync(command);
            var command2 = new Command(CommandType.Method, "Command2", new object[] { "Riccaro Chirra" }, null);
            await adapter.WriteCommandAsync(command2);
            adapter.CloseCommandLog();

            //ASSERT
            var files = Directory.GetFiles(dataPath);
            files.Count().Should().Be(1);
            files.First().Should().EndWith("00000000000000000001.commandlog");
            string commandlog = File.ReadAllText(Path.Combine(dataPath, "00000000000000000001.commandlog"));
            IEnumerable<string> jsons = new JsonCommandlogHelper(commandlog).GetCommandJsons();
            jsons.First().Should().Be("{\"$type\":\"BoCode.RedoDB.Persistence.Commands.Command, BoCode.RedoDB\",\"CommandType\":0,\"MemberName\":\"Command1\",\"Args\":[\"2021-07-13T00:00:00\"],\"CommandContext\":null}");

            jsons.ElementAt(1).Should().Be("{\"$type\":\"BoCode.RedoDB.Persistence.Commands.Command, BoCode.RedoDB\",\"CommandType\":0,\"MemberName\":\"Command2\",\"Args\":[\"Riccaro Chirra\"],\"CommandContext\":null}");
        }

        [Fact(DisplayName = "Get commands to recover without the presence of a snapshot")]
        public void Test3()
        {
            //ARRANGE
            string dataPath = NewDataPath;
            EnsureDataPath(dataPath);
            string commandlog = @"{""CommandType"":""0"",""MemberName"":""Command1"",""Args"":[""2021-07-13T00:00:00""],""CommandContext"":null}" +
                    @"{""CommandType"":""0"",""MemberName"":""Command2"",""Args"":[""Riccaro Chirra""],""CommandContext"":null}";
            File.WriteAllText(Path.Combine(dataPath, "00000000000000000001.commandlog"), commandlog);

            //ACT
            JsonCommandAdapter adapter = new JsonCommandAdapter(new(dataPath), new CommandLogNameProvider());
            adapter.LastSnapshotName = string.Empty;
            var recoveringLogs = adapter.RecoveringLogs;

            recoveringLogs.Count().Should().Be(1);
            recoveringLogs.First().Content.Should().Be(commandlog);
            recoveringLogs.First().Name.Should().Be("00000000000000000001.commandlog");
        }

        [Fact(DisplayName = "Calling RecoveringLogs without LastSnapshotName set on adapter raises RedoDBEngineException")]
        public void Test4()
        {
            //ARRANGE
            string dataPath = NewDataPath;
            EnsureDataPath(dataPath);
            string commandlog = @"{""CommandType"":""0"",""MemberName"":""Command1"",""Args"":[""2021-07-13T00:00:00""],""CommandContext"":null}" +
                    @"{""CommandType"":""0"",""MemberName"":""Command2"",""Args"":[""Riccaro Chirra""],""CommandContext"":null}";
            File.WriteAllText(Path.Combine(dataPath, "00000000000000000001.commandlog"), commandlog);
            IEnumerable<Commandlog> recoveringLogs = null;

            //ACT
            JsonCommandAdapter adapter = new JsonCommandAdapter(new(dataPath), new CommandLogNameProvider());
            Action lastSnapshotName = () => recoveringLogs = adapter.RecoveringLogs;

            lastSnapshotName.Should().Throw<RedoDBEngineException>();
        }

        //if the commandlog has the same number as the snapshot, the snapshot already includes all commands from this commandlog
        [Fact(DisplayName = "GIVEN a commandlog with the same ordinal as LastSnapshotName is present " +
            "WHEN I ask for RecoveryLogs " +
            "THEN an empty list is returned.")]
        public void Test5()
        {
            //ARRANGE
            string dataPath = NewDataPath;
            EnsureDataPath(dataPath);
            string commandlog = @"{""MemberName"":""Command1"",""Args"":[""2021-07-13T00:00:00""],""CommandContext"":null}" +
                    @"{""MemberName"":""Command2"",""Args"":[""Riccaro Chirra""],""CommandContext"":null}";
            File.WriteAllText(Path.Combine(dataPath, "00000000000000000001.commandlog"), commandlog);

            string commandlog2 = @"{""MemberName"":""Command3"",""Args"":[""2021-07-13T00:00:00""],""CommandContext"":null}" +
                @"{""MemberName"":""Command4"",""Args"":[""Riccaro Chirra""],""CommandContext"":null}";
            File.WriteAllText(Path.Combine(dataPath, "00000000000000000002.commandlog"), commandlog2);

            //ACT
            JsonCommandAdapter adapter = new JsonCommandAdapter(new(dataPath), new CommandLogNameProvider());
            adapter.LastSnapshotName = "00000000000000000001.snapshot";
            var recoveringLogs = adapter.RecoveringLogs;

            recoveringLogs.Count().Should().Be(1);
            recoveringLogs.Single().Name.Should().Be("00000000000000000002.commandlog");
        }

        //if the commandlog has the same number as the snapshot, the snapshot already includes all commands from this commandlog
        [Fact(DisplayName = "GIVEN two commandlogs with higher ordinal as LastSnapshotName " +
            "WHEN I ask for RecoveryLogs " +
            "THEN both are in the RecoveringLogs list.")]
        public void Test6()
        {
            //ARRANGE
            string dataPath = NewDataPath;
            EnsureDataPath(dataPath);
            string commandlog = @"{""MemberName"":""Command1"",""Args"":[""2021-07-13T00:00:00""],""CommandContext"":null}" +
                    @"{""MemberName"":""Command2"",""Args"":[""Riccaro Chirra""],""CommandContext"":null}";
            File.WriteAllText(Path.Combine(dataPath, "00000000000000000001.commandlog"), commandlog);

            string commandlog2 = @"{""MemberName"":""Command3"",""Args"":[""2021-07-13T00:00:00""],""CommandContext"":null}" +
                @"{""MemberName"":""Command4"",""Args"":[""Riccaro Chirra""],""CommandContext"":null}";
            File.WriteAllText(Path.Combine(dataPath, "00000000000000000002.commandlog"), commandlog2);

            string commandlog3 = @"{""MemberName"":""Command3"",""Args"":[""2021-07-13T00:00:00""],""CommandContext"":null}" +
               @"{""MemberName"":""Command4"",""Args"":[""Riccaro Chirra""],""CommandContext"":null}";
            File.WriteAllText(Path.Combine(dataPath, "00000000000000000003.commandlog"), commandlog3);

            //ACT
            JsonCommandAdapter adapter = new JsonCommandAdapter(new(dataPath), new CommandLogNameProvider());
            adapter.LastSnapshotName = "00000000000000000001.snapshot";
            var recoveringLogs = adapter.RecoveringLogs;

            recoveringLogs.Count().Should().Be(2);
            recoveringLogs.First().Name.Should().Be("00000000000000000002.commandlog");
            recoveringLogs.Last().Name.Should().Be("00000000000000000003.commandlog");
        }

        //if the commandlog has the same number as the snapshot, the snapshot already includes all commands from this commandlog
        [Fact(DisplayName = "GIVEN a commandlog with lower ordinal then the snapshot " +
            "WHEN I ask for RecoveryLogs " +
            "THEN an empty list is returned.")]
        public void Test7()
        {
            //ARRANGE
            string dataPath = NewDataPath;
            EnsureDataPath(dataPath);
            string commandlog = @"{""MemberName"":""Command1"",""Args"":[""2021-07-13T00:00:00""],""CommandContext"":null}" +
                    @"{""MemberName"":""Command2"",""Args"":[""Riccaro Chirra""],""CommandContext"":null}";
            File.WriteAllText(Path.Combine(dataPath, "00000000000000000001.commandlog"), commandlog);

            //ACT
            JsonCommandAdapter adapter = new JsonCommandAdapter(new(dataPath), new CommandLogNameProvider());
            adapter.LastSnapshotName = "00000000000000000003.snapshot";
            var recoveringLogs = adapter.RecoveringLogs;

            recoveringLogs.Count().Should().Be(0);
        }
    }
}
