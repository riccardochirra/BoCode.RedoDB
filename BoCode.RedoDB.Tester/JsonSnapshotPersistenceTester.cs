using BoCode.RedoDB.Persistence;
using BoCode.RedoDB.Persistence.Snapshots;
using BoCode.RedoDB.Tester.Infrastructure;
using FluentAssertions;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace BoCode.RedoDB.Tester
{
    [Serializable]
    public class Redoable
    {
        public string AString { get; set; }
        public int AnInt { get; set; }
        public DateTime ADate { get; set; }
    }

    [Collection("Test Directory Collection")]
    public class JsonSnapshotPersistenceTester : TesterWithDataPath
    {
        public JsonSnapshotPersistenceTester(ITestOutputHelper output) : base(output) { }

        [Fact(DisplayName = "Serialize a redoable")]
        public void Test1()
        {
            //ARRANGE
            string newDataPath = base.NewDataPath;
            JsonSnapshotAdapter<Redoable> persister = new(new DirectoryInfo(newDataPath), new SnapshotNameProvider());
            Redoable anObject = new Redoable() { AString = "Ciao", AnInt = 10, ADate = new DateTime(2021, 7, 7) };

            //ACT
            persister.Serialize(anObject);

            //ASSERT
            new DirectoryInfo(newDataPath).GetFiles().Count().Should().Be(1);
        }

        [Fact(DisplayName = "Deserialize a redoable")]
        public void Test2()
        {
            //ARRANGE
            JsonSnapshotAdapter<Redoable> persister = new(new DirectoryInfo(NewDataPath), new SnapshotNameProvider());
            Redoable anObject = new Redoable() { AString = "Ciao", AnInt = 10, ADate = new DateTime(2021, 7, 7) };
            persister.Serialize(anObject);

            //ACT
            Redoable deserializedObject = persister.Deserialize();

            //ASSERT
            deserializedObject.AString.Should().Be("Ciao");
            deserializedObject.AnInt.Should().Be(10);
            deserializedObject.ADate.Should().Be(new DateTime(2021, 7, 7));
        }

        [Fact(DisplayName = "Deserialize a redoable async")]
        public async Task Test3()
        {
            //ARRANGE
            JsonSnapshotAdapter<Redoable> persister = new(new DirectoryInfo(NewDataPath), new SnapshotNameProvider());
            Redoable anObject = new Redoable() { AString = "Ciao", AnInt = 10, ADate = new DateTime(2021, 7, 7) };
            persister.Serialize(anObject);

            //ACT
            Redoable deserializedObject = await persister.DeserializeAsync();

            //ASSERT
            deserializedObject.AString.Should().Be("Ciao");
            deserializedObject.AnInt.Should().Be(10);
            deserializedObject.ADate.Should().Be(new DateTime(2021, 7, 7));
        }
    }
}
