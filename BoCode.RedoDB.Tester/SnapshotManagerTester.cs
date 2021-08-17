using BoCode.RedoDB.Persistence;
using BoCode.RedoDB.Persistence.Snapshots;
using BoCode.RedoDB.RedoableSystem;
using BoCode.RedoDB.Tester.Infrastructure;
using FluentAssertions;
using System.IO;
using System.Linq;
using Xunit;

namespace BoCode.RedoDB.Tester
{
    public class SnapshotManagerTester : TesterWithDataPath
    {
        [Fact(DisplayName = "Last snapshot can't be found")]
        public void Test1()
        {
            //ARRANGE create a dataPath folder 
            DirectoryInfo directoryInfo = new DirectoryInfo(NewDataPath);
            directoryInfo.Create();

            //ACT, ASSERT
            SnapshotManager<ContactsSystem> snapshotManager = new(null, new JsonSnapshotAdapter<ContactsSystem>(directoryInfo, new SnapshotNameProvider()));
            snapshotManager.LastSnapshot.Should().BeNull();
        }

        [Fact(DisplayName = "Last snapshot can be found")]
        public void Test2()
        {
            //ARRANGE create a dataPath folder and add some file to it.
            DirectoryInfo directoryInfo = new DirectoryInfo(NewDataPath);
            directoryInfo.Create();
            using var fileStream = File.Create(Path.Combine(directoryInfo.FullName, "00000000000000000001.snapshot"));

            //ACT, ASSERT
            SnapshotManager<ContactsSystem> snapshotManager = new(null, new JsonSnapshotAdapter<ContactsSystem>(directoryInfo, new SnapshotNameProvider()));
            snapshotManager.LastSnapshot.Should().NotBeNull();
        }

        [Fact(DisplayName = "TakeSnapshot creates the folder and the snapshot file.")]
        public void Test3()
        {
            //ARRANGE create some data to serialize
            DirectoryInfo directoryInfo = new DirectoryInfo(NewDataPath);
            ContactsSystem contacts = new();
            contacts.AddContact(new());
            contacts.AddContact(new());

            //ACT,
            SnapshotManager<ContactsSystem> snapshotManager = new(() => contacts, new JsonSnapshotAdapter<ContactsSystem>(directoryInfo, new SnapshotNameProvider()));
            snapshotManager.TakeSnapshot();

            //ASSERT
            snapshotManager.LastSnapshot.Should().NotBeNull();
        }

        [Fact(DisplayName = "GIVEN I have already a snapshot '00000000000000000001.snapshot WHEN I take a second snapshot THEN the name of the file is '00000000000000000002.snapshot")]
        public void Test4()
        {
            //ARRANGE create some data to serialize
            DirectoryInfo directoryInfo = new DirectoryInfo(NewDataPath);
            ContactsSystem contacts = new();
            contacts.AddContact(new());
            contacts.AddContact(new());

            //ACT,
            SnapshotManager<ContactsSystem> snapshotManager = new(() => contacts, new JsonSnapshotAdapter<ContactsSystem>(directoryInfo, new SnapshotNameProvider()));
            snapshotManager.TakeSnapshot();
            contacts.AddContact(new() { Name = "Contact4" });
            snapshotManager.TakeSnapshot();

            //ASSERT
            var files = directoryInfo
                .GetFiles("*.snapshot")
                .OrderBy(f => f.FullName)
                .ToList();

            files.Should().HaveCount(2);
            
            files[^1].Name.Should().Be("00000000000000000002.snapshot");
        }
    }
}
