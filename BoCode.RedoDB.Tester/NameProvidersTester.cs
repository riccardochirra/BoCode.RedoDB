using BoCode.RedoDB.Persistence;
using BoCode.RedoDB.Persistence.Commands;
using BoCode.RedoDB.Persistence.Snapshots;
using FluentAssertions;
using Xunit;

namespace BoCode.RedoDB.Tester
{

    public class NameProvidersTester
    {
        [Fact(DisplayName = "New snapshot file name for 00000000000000000001.snapshot is 00000000000000000002.snapshot")]
        public void Test1()
        {
            SnapshotNameProvider sut = new SnapshotNameProvider();
            sut.NewName("00000000000000000001.snapshot").Should().Be("00000000000000000002.snapshot");
        }

        [Fact(DisplayName = "New log file name for 00000000000000000001.snapshot is 00000000000000000002.snapshot")]
        public void Test2()
        {
            CommandlogNameProvider sut = new CommandlogNameProvider();
            sut.NewName("00000000000000000001.commandlog").Should().Be("00000000000000000002.commandlog");
        }

        [Fact(DisplayName = "First snapshot name is 00000000000000000001.snapshot")]
        public void Test3()
        {
            SnapshotNameProvider sut = new SnapshotNameProvider();
            sut.FirstName.Should().Be("00000000000000000001.snapshot");
        }

        [Fact(DisplayName = "First log name is 00000000000000000001.snapshot")]
        public void Test4()
        {
            CommandlogNameProvider sut = new CommandlogNameProvider();
            sut.FirstName.Should().Be("00000000000000000001.commandlog");
        }

        [Fact(DisplayName = "NewName with ordinal genertes a new name with the ordinal incremented")]
        public void Test5()
        {
            SnapshotNameProvider sut = new SnapshotNameProvider();
            sut.NewName(1).Should().Be("00000000000000000002.snapshot");
        }

        [Fact(DisplayName = "New log file name for 00000000000000000001.snapshot is 00000000000000000002.snapshot")]
        public void Test6()
        {
            CommandlogNameProvider sut = new CommandlogNameProvider();
            sut.NewName("1").Should().Be("00000000000000000002.commandlog");
        }

    }
}
